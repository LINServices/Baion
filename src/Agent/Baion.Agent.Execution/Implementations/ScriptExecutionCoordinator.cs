using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Baion.Agent.Core;
using Baion.Contracts.Enums;
using Baion.Contracts.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Agent.Execution.Implementations;

/// <summary>
/// Lleva las ejecuciones en paralelo hasta el tope que fija el orquestador. <see cref="Enqueue"/> vuelve
/// de inmediato y el trabajo real corre en segundo plano: si esperara un hueco, un agente saturado dejaría
/// de leer su socket y parecería caído.
/// </summary>
internal class ScriptExecutionCoordinator(IScriptExecutor executor, IOrchestratorChannel channel, IOptions<ScriptExecutionOptions> options, TimeProvider timeProvider, ILogger<ScriptExecutionCoordinator> logger) : IScriptExecutionCoordinator, IHostedService
{
    private readonly ConcurrentDictionary<Guid, Task> _running = new();

    private readonly ConcurrentDictionary<Guid, Counter> _sequences = new();

    private readonly CancellationTokenSource _shutdown = new();

    private readonly Lock _slotsLock = new();

    private SemaphoreSlim _slots = new(1, 1);

    private int _configuredLimit = 1;

    public int RunningCount => _running.Count;

    public void Enqueue(ExecuteScriptMessage request)
    {
        // El identificador de ejecución es la clave de idempotencia: un reenvío del orquestador tras
        // una reconexión no puede acabar lanzando el script dos veces.
        if (_running.ContainsKey(request.ExecutionId))
        {
            logger.LogWarning("La ejecución {ExecutionId} ya está en curso; se ignora el reenvío", request.ExecutionId);
            return;
        }

        _running[request.ExecutionId] = Task.Run(() => RunAsync(request), CancellationToken.None);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ExecutionWorkspace.Sweep(options.Value, StaleWorkspaceAge, timeProvider, logger);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _shutdown.CancelAsync();

        // Se da margen a que lo que está en curso informe su desenlace, sin bloquear la parada del host.
        await Task.WhenAny(Task.WhenAll([.. _running.Values]), Task.Delay(Timeout.Infinite, cancellationToken));
    }

    private async Task RunAsync(ExecuteScriptMessage request)
    {
        var slots = ResolveSlots();

        try
        {
            await slots.WaitAsync(_shutdown.Token);
        }
        catch (OperationCanceledException)
        {
            await ReportAsync(new ScriptCompletedMessage(request.ExecutionId, ExecutionStatus.Canceled, null, timeProvider.GetUtcNow(), "El agente se estaba deteniendo cuando llegó la orden."));
            Forget(request.ExecutionId);
            return;
        }

        try
        {
            var outcome = await executor.ExecuteAsync(request, ReportStartedAsync, ReportOutputAsync, _shutdown.Token);
            await ReportAsync(new ScriptCompletedMessage(request.ExecutionId, outcome.Status, outcome.ExitCode, timeProvider.GetUtcNow(), outcome.ErrorMessage));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "La ejecución {ExecutionId} terminó con una excepción no controlada", request.ExecutionId);
            await ReportAsync(new ScriptCompletedMessage(request.ExecutionId, ExecutionStatus.Failed, null, timeProvider.GetUtcNow(), exception.Message));
        }
        finally
        {
            slots.Release();
            Forget(request.ExecutionId);
        }

        Task ReportStartedAsync(int processId) => ReportAsync(new ScriptStartedMessage(request.ExecutionId, timeProvider.GetUtcNow(), processId));

        Task ReportOutputAsync(OutputStream stream, string content) => ReportAsync(new ScriptOutputChunkMessage(request.ExecutionId, stream, NextSequence(request.ExecutionId), content));
    }

    /// <summary>
    /// El tope lo manda el orquestador en la bienvenida, así que puede cambiar entre sesiones y el semáforo
    /// se rehace cuando lo hace. Las ejecuciones ya en marcha conservan el suyo y lo liberan sobre él.
    /// </summary>
    private SemaphoreSlim ResolveSlots()
    {
        var limit = Math.Max(channel.Session?.MaxConcurrentExecutions ?? options.Value.FallbackMaxConcurrentExecutions, 1);

        lock (_slotsLock)
        {
            if (limit != _configuredLimit)
            {
                _slots = new SemaphoreSlim(limit, limit);
                _configuredLimit = limit;
                logger.LogInformation("Tope de ejecuciones simultáneas fijado en {Tope}", limit);
            }

            return _slots;
        }
    }

    private long NextSequence(Guid executionId) => Interlocked.Increment(ref _sequences.GetOrAdd(executionId, _ => new Counter()).Value);

    private void Forget(Guid executionId)
    {
        _running.TryRemove(executionId, out _);
        _sequences.TryRemove(executionId, out _);
    }

    private async Task ReportAsync(AgentToServerMessage message)
    {
        if (!await channel.TrySendAsync(message, CancellationToken.None))
        {
            logger.LogWarning("No se pudo reportar {MessageType}: no hay sesión con el orquestador", message.GetType().Name);
        }
    }

    /// <summary>Margen antes de recoger la carpeta de una ejecución; cubre de sobra a los procesos Detached largos.</summary>
    private static readonly TimeSpan StaleWorkspaceAge = TimeSpan.FromDays(1);

    /// <summary>Contador por ejecución; <c>Interlocked</c> necesita una referencia estable sobre la que operar.</summary>
    private sealed class Counter
    {
        public long Value;
    }
}
