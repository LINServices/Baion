using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Messages;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Services;

/// <summary>
/// Añade un camino de petición/respuesta sobre el socket del agente, que de suyo es de una sola dirección.
/// El orquestador manda una <see cref="ServerToAgentMessage"/> con un <c>RequestId</c> y espera la
/// <see cref="IAgentQueryReply"/> que lo lleve de vuelta.
/// </summary>
/// <remarks>
/// Solo alcanza a agentes conectados a <b>esta</b> instancia: el enrutado de respuestas entre instancias
/// es de una fase posterior (la de multi-instancia). Si el socket vive en otra instancia, la consulta
/// falla con <c>agent.not_reachable</c>.
/// </remarks>
public interface IAgentQueryDispatcher
{
    /// <summary>
    /// Envía la petición al agente del servidor y espera su respuesta correlacionada, o un error si el
    /// agente no está en esta instancia, no responde a tiempo o el envío falla.
    /// </summary>
    Task<Result<AgentToServerMessage>> QueryAsync(Guid serverId, Guid requestId, ServerToAgentMessage request, CancellationToken cancellationToken);

    /// <summary>
    /// Casa una respuesta del agente con la petición que la espera. La invoca el hilo del socket, así que
    /// no bloquea: solo completa la espera pendiente, si la hay.
    /// </summary>
    void TryComplete(AgentToServerMessage reply);
}
