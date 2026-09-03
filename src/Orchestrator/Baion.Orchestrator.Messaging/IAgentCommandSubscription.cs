using System;
using System.Threading;
using System.Threading.Tasks;

namespace Baion.Orchestrator.Messaging;

/// <summary>
/// Suscripción de esta instancia a los comandos de los agentes que tiene conectados. Se enlaza al aceptar
/// un socket y se suelta al cerrarse, de modo que la clave de enrutado solo existe donde vive la conexión.
/// </summary>
public interface IAgentCommandSubscription
{
    /// <summary>Empieza a recibir los comandos dirigidos a este servidor.</summary>
    Task SubscribeAsync(Guid serverId, CancellationToken cancellationToken);

    /// <summary>Deja de recibirlos.</summary>
    Task UnsubscribeAsync(Guid serverId, CancellationToken cancellationToken);
}
