using LIN.Cloud.Enterprise.Baion.Contracts.Messages;

namespace LIN.Cloud.Enterprise.Baion.Agent.Services;

/// <summary>
/// Atiende las peticiones de servicios del orquestador fuera del hilo del socket: la consulta se
/// resuelve en segundo plano y se responde con el mismo <c>RequestId</c> que traía la petición.
/// </summary>
public interface IServiceQueryProcessor
{
    /// <summary>
    /// Acepta una petición de servicios y vuelve de inmediato; la procesa en segundo plano.
    /// </summary>
    void Enqueue(ServerToAgentMessage request);
}
