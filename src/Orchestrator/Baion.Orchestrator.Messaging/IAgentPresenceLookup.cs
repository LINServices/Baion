using System;
using System.Threading;
using System.Threading.Tasks;

namespace Baion.Orchestrator.Messaging;

/// <summary>
/// Resuelve en qué instancia vive el socket de un servidor. La declara la capa de mensajería y la implementa
/// la de persistencia sobre las columnas que el propio handshake ya mantiene.
/// </summary>
public interface IAgentPresenceLookup
{
    /// <summary>Instancia que tiene el socket, o null si el agente no está conectado en ninguna.</summary>
    Task<string?> ResolveInstanceAsync(Guid serverId, CancellationToken cancellationToken);
}
