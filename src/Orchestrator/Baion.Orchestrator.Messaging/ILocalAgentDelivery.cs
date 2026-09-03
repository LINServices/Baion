using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Messages;

namespace Baion.Orchestrator.Messaging;

/// <summary>
/// Entrega a los sockets abiertos en <b>este</b> proceso. La declara la capa de mensajería y la implementa
/// la de servicios: así el consumidor de RabbitMQ puede entregar lo que recibe sin que esta capa dependa
/// del registro de conexiones.
/// </summary>
public interface ILocalAgentDelivery
{
    /// <summary>Entrega el mensaje si el socket del servidor está en esta instancia.</summary>
    Task<bool> TryDeliverAsync(Guid serverId, ServerToAgentMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Cierra la conexión local de un servidor que acaba de aparecer en otra instancia. Quien implementa
    /// esto sabe su propia identidad, así que puede ignorar el aviso que él mismo emitió.
    /// </summary>
    Task EvictAsync(Guid serverId, string claimedByInstanceId);
}
