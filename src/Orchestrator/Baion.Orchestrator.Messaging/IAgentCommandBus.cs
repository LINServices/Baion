using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Messages;

namespace Baion.Orchestrator.Messaging;

/// <summary>
/// Lleva un comando hasta el agente, esté conectado a la instancia que sea. Primero intenta la entrega
/// local; si el socket vive en otra instancia, publica con la clave de enrutado del servidor y RabbitMQ
/// se encarga de que solo llegue a quien lo tiene.
/// </summary>
public interface IAgentCommandBus
{
    /// <summary>Envía el comando. Devuelve false si el agente no está conectado en ninguna instancia.</summary>
    Task<bool> TrySendAsync(Guid serverId, ServerToAgentMessage message, CancellationToken cancellationToken);
}
