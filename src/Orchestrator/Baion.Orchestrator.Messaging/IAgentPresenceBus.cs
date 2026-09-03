using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;

namespace Baion.Orchestrator.Messaging;

/// <summary>Anuncia a todas las instancias que un agente se conectó o se fue.</summary>
public interface IAgentPresenceBus
{
    /// <summary>Publica el cambio de presencia en el exchange fanout.</summary>
    Task PublishAsync(AgentPresenceChanged notification, CancellationToken cancellationToken);
}
