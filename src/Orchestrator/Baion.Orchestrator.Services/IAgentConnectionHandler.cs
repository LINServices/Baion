using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;

namespace Baion.Orchestrator.Services;

/// <summary>Atiende un socket de agente ya aceptado: handshake, bucle de mensajes y cierre.</summary>
public interface IAgentConnectionHandler
{
    /// <summary>Conduce la conexión hasta que el agente cierre o se cancele.</summary>
    Task HandleAsync(WebSocket socket, AgentCredentialContext credentials, CancellationToken cancellationToken);
}
