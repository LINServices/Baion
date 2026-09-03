using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Messages;

namespace Baion.Agent.Core;

/// <summary>
/// Punto de envío hacia el orquestador para el resto de capas del agente. Existe para que quien reporta
/// métricas o resultados de ejecución no tenga que conocer el socket ni su ciclo de vida.
/// </summary>
public interface IOrchestratorChannel
{
    /// <summary>Indica si hay una sesión establecida en este momento.</summary>
    bool IsConnected { get; }

    /// <summary>Parámetros de la sesión en curso, o null si no hay ninguna.</summary>
    AgentSessionInfo? Session { get; }

    /// <summary>Envía un mensaje. Devuelve false si no hay sesión o si el envío falló.</summary>
    Task<bool> TrySendAsync(AgentToServerMessage message, CancellationToken cancellationToken);
}
