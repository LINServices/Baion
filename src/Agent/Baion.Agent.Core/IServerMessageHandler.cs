using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Messages;

namespace Baion.Agent.Core;

/// <summary>
/// Atiende un tipo de mensaje del orquestador. Cada capa del agente registra los suyos, de modo que
/// el bucle de conexión no tenga que conocer ni la ejecución de scripts ni la auto-actualización.
/// </summary>
public interface IServerMessageHandler
{
    /// <summary>Indica si este manejador se hace cargo del mensaje.</summary>
    bool CanHandle(ServerToAgentMessage message);

    /// <summary>Procesa el mensaje. No debe bloquear el bucle de recepción.</summary>
    Task HandleAsync(ServerToAgentMessage message, CancellationToken cancellationToken);
}
