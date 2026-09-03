using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Messages;
using Baion.Orchestrator.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Orchestrator.Services.Implementations;

/// <summary>
/// Puente entre la capa de mensajería y los sockets abiertos en este proceso. La mensajería declara la
/// interfaz y no conoce el registro de conexiones; el registro no conoce RabbitMQ.
/// </summary>
internal class LocalAgentDelivery(IAgentRegistry registry, IOptions<OrchestratorOptions> options, ILogger<LocalAgentDelivery> logger) : ILocalAgentDelivery
{
    public async Task<bool> TryDeliverAsync(Guid serverId, ServerToAgentMessage message, CancellationToken cancellationToken)
    {
        if (!registry.TryGet(serverId, out var connection))
        {
            return false;
        }

        try
        {
            await connection.SendAsync(message, cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogWarning("Falló la entrega local al servidor {ServerId}: {Motivo}", serverId, exception.Message);
            return false;
        }
    }

    public async Task EvictAsync(Guid serverId, string claimedByInstanceId)
    {
        // El aviso que emitió esta misma instancia vuelve por el fanout: desalojar aquí cerraría
        // justamente la conexión buena que se acaba de establecer.
        if (claimedByInstanceId == options.Value.InstanceId)
        {
            return;
        }

        if (!registry.TryGet(serverId, out var connection))
        {
            return;
        }

        logger.LogWarning("El servidor {ServerId} reapareció en la instancia {InstanceId}; se cierra la conexión que quedaba aquí", serverId, claimedByInstanceId);
        registry.Remove(connection);

        await connection.CloseAsync("El agente reconectó en otra instancia.");
    }
}
