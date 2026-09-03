using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Messages;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Services;

/// <summary>Handshake del agente: valida credenciales, registra el servidor y mantiene su presencia.</summary>
public interface IAgentEnrollmentService
{
    /// <summary>Valida las credenciales de la cabecera. Se llama antes de aceptar el socket, para poder rechazar con 401.</summary>
    Task<Result<AgentCredentialContext>> ResolveCredentialsAsync(AgentCredentials credentials, CancellationToken cancellationToken);

    /// <summary>Registra o actualiza el servidor con los datos del saludo y abre la sesión.</summary>
    Task<Result<AgentSession>> CompleteHandshakeAsync(AgentCredentialContext context, HelloMessage hello, CancellationToken cancellationToken);

    /// <summary>Refresca la última señal de vida del servidor.</summary>
    Task TouchAsync(Guid tenantId, Guid serverId, CancellationToken cancellationToken);

    /// <summary>Marca el servidor como desconectado al cerrarse el socket.</summary>
    Task MarkDisconnectedAsync(Guid tenantId, Guid serverId, CancellationToken cancellationToken);
}
