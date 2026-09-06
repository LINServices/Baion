using Baion.Contracts.Messages;

namespace Baion.Agent.Services;

/// <summary>
/// Atiende las peticiones de servicios del orquestador fuera del hilo del socket: <see cref="Enqueue"/>
/// vuelve de inmediato y la consulta se resuelve en segundo plano, respondiendo con el mismo
/// <c>RequestId</c> que traía la petición.
/// </summary>
public interface IServiceQueryProcessor
{
    /// <summary>Acepta una petición de servicios y la procesa en segundo plano.</summary>
    void Enqueue(ServerToAgentMessage request);
}
