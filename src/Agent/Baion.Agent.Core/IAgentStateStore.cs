using System.Threading;
using System.Threading.Tasks;

namespace Baion.Agent.Core;

/// <summary>Guarda y recupera el estado del agente entre arranques.</summary>
public interface IAgentStateStore
{
    /// <summary>Carga el estado. Si no existe, devuelve uno nuevo con un identificador de máquina recién resuelto.</summary>
    Task<AgentState> LoadAsync(CancellationToken cancellationToken);

    /// <summary>Persiste el estado con permisos restringidos: contiene la credencial del agente.</summary>
    Task SaveAsync(AgentState state, CancellationToken cancellationToken);
}
