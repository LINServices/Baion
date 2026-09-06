using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Baion.Agent.Core;
using Baion.Contracts.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Baion.Agent.Services.Implementations;

/// <summary>
/// Lleva cada petición de servicios en su propia tarea: <see cref="Enqueue"/> no espera nada porque, si lo
/// hiciera, un agente ocupado dejaría de leer su socket y parecería caído. Al parar el host se da un margen
/// a que las consultas en curso respondan.
/// </summary>
internal class ServiceQueryProcessor(IServiceInspector inspector, IOrchestratorChannel channel, ILogger<ServiceQueryProcessor> logger) : IServiceQueryProcessor, IHostedService
{
    private readonly ConcurrentDictionary<Guid, Task> _running = new();

    private readonly CancellationTokenSource _shutdown = new();

    public void Enqueue(ServerToAgentMessage request)
    {
        var requestId = RequestIdOf(request);

        if (requestId == Guid.Empty)
        {
            logger.LogWarning("Se ignora una petición de servicios de tipo inesperado: {Tipo}", request.GetType().Name);
            return;
        }

        // El RequestId hace idempotente un reenvío del orquestador tras una reconexión.
        if (!_running.TryAdd(requestId, Task.CompletedTask))
        {
            logger.LogWarning("La petición de servicios {RequestId} ya está en curso; se ignora el reenvío", requestId);
            return;
        }

        _running[requestId] = Task.Run(() => ProcessAsync(request, requestId), CancellationToken.None);
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _shutdown.CancelAsync();
        await Task.WhenAny(Task.WhenAll([.. _running.Values]), Task.Delay(Timeout.Infinite, cancellationToken));
    }

    private async Task ProcessAsync(ServerToAgentMessage request, Guid requestId)
    {
        try
        {
            var reply = await BuildReplyAsync(request, _shutdown.Token);

            if (!await channel.TrySendAsync(reply, CancellationToken.None))
            {
                logger.LogWarning("No se pudo responder a la petición de servicios {RequestId}: no hay sesión con el orquestador", requestId);
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            logger.LogInformation("La petición de servicios {RequestId} se abandonó porque el agente se está deteniendo", requestId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "La petición de servicios {RequestId} terminó con una excepción no controlada", requestId);
            await channel.TrySendAsync(new ServiceQueryFailedMessage(requestId, ServiceQueryFailedMessage.ToolFailedCode, exception.Message), CancellationToken.None);
        }
        finally
        {
            _running.TryRemove(requestId, out _);
        }
    }

    private async Task<AgentToServerMessage> BuildReplyAsync(ServerToAgentMessage request, CancellationToken cancellationToken) => request switch
    {
        ListServicesRequestMessage message => Reply(
            message.RequestId,
            await inspector.ListAsync(message.NameFilter, cancellationToken),
            services => new ServicesListedMessage(message.RequestId, services)),

        DescribeServiceRequestMessage message => Reply(
            message.RequestId,
            await inspector.DescribeAsync(message.ServiceId, cancellationToken),
            detail => new ServiceDescribedMessage(message.RequestId, detail)),

        ServiceLogsRequestMessage message => Reply(
            message.RequestId,
            await inspector.GetLogsAsync(message.ServiceId, message.MaxLines, message.Since, cancellationToken),
            page => new ServiceLogsMessage(message.RequestId, page.Lines, page.Truncated)),

        ControlServiceRequestMessage message => Reply(
            message.RequestId,
            await inspector.ControlAsync(message.ServiceId, message.Action, cancellationToken),
            detail => new ServiceControlledMessage(message.RequestId, detail)),

        _ => throw new InvalidOperationException($"El procesador de servicios no atiende {request.GetType().Name}.")
    };

    private static AgentToServerMessage Reply<T>(Guid requestId, ServiceQueryOutcome<T> outcome, Func<T, AgentToServerMessage> onSuccess)
        => outcome.IsSuccess
            ? onSuccess(outcome.Value!)
            : new ServiceQueryFailedMessage(requestId, outcome.FailureCode!, outcome.FailureMessage!);

    private static Guid RequestIdOf(ServerToAgentMessage request) => request switch
    {
        ListServicesRequestMessage message => message.RequestId,
        DescribeServiceRequestMessage message => message.RequestId,
        ServiceLogsRequestMessage message => message.RequestId,
        ControlServiceRequestMessage message => message.RequestId,
        _ => Guid.Empty
    };
}
