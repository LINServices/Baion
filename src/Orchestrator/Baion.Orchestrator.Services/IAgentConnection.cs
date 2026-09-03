using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Messages;

namespace Baion.Orchestrator.Services;

/// <summary>Canal abierto hacia un agente conectado a esta instancia.</summary>
public interface IAgentConnection
{
    Guid TenantId { get; }

    Guid ServerId { get; }

    /// <summary>Envía un mensaje al agente por el socket ya abierto.</summary>
    Task SendAsync(ServerToAgentMessage message, CancellationToken cancellationToken);

    /// <summary>Cierra el socket indicando el motivo.</summary>
    Task CloseAsync(string reason);
}
