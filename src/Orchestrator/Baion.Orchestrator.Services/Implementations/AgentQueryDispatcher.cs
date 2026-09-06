using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Messages;
using Baion.Orchestrator.Models.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Orchestrator.Services.Implementations;

/// <summary>
/// Guarda una espera por cada petición en vuelo y la completa cuando llega la respuesta con el mismo
/// <c>RequestId</c>. Singleton: el hilo del socket y quien consulta viven en scopes distintos.
/// </summary>
internal sealed class AgentQueryDispatcher(IAgentRegistry registry, IOptions<AgentQueryOptions> options, ILogger<AgentQueryDispatcher> logger) : IAgentQueryDispatcher
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<AgentToServerMessage>> _pending = new();

    public async Task<Result<AgentToServerMessage>> QueryAsync(Guid serverId, Guid requestId, ServerToAgentMessage request, CancellationToken cancellationToken)
    {
        if (!registry.TryGet(serverId, out var connection))
        {
            return Result<AgentToServerMessage>.Failure(Error.Conflict("agent.not_reachable", "El agente del servidor no está conectado a esta instancia."));
        }

        // RunContinuationsAsynchronously: TryComplete corre en el hilo del socket y no debe arrastrar ahí
        // la continuación de quien esperaba.
        var espera = new TaskCompletionSource<AgentToServerMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_pending.TryAdd(requestId, espera))
        {
            return Result<AgentToServerMessage>.Failure(Error.Unexpected("agent.query_duplicate", "Ya hay una petición en curso con ese identificador."));
        }

        try
        {
            await connection.SendAsync(request, cancellationToken);

            var respuesta = await espera.Task.WaitAsync(TimeSpan.FromSeconds(options.Value.TimeoutSeconds), cancellationToken);
            return Result<AgentToServerMessage>.Success(respuesta);
        }
        catch (TimeoutException)
        {
            logger.LogWarning("El agente del servidor {ServerId} no respondió a la petición {RequestId} en {Timeout}s", serverId, requestId, options.Value.TimeoutSeconds);
            return Result<AgentToServerMessage>.Failure(Error.Conflict("agent.query_timeout", "El agente no respondió a tiempo."));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falló el envío de la petición {RequestId} al servidor {ServerId}", requestId, serverId);
            return Result<AgentToServerMessage>.Failure(Error.Unexpected("agent.query_failed", "No se pudo consultar al agente."));
        }
        finally
        {
            _pending.TryRemove(requestId, out _);
        }
    }

    public void TryComplete(AgentToServerMessage reply)
    {
        if (reply is IAgentQueryReply correlacionada && _pending.TryGetValue(correlacionada.RequestId, out var espera))
        {
            espera.TrySetResult(reply);
        }
    }
}
