using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Messaging.Implementations;

// Sin más instancias a las que avisar, la presencia no viaja a ninguna parte.
internal class NoOpAgentPresenceBus : IAgentPresenceBus
{
    public Task PublishAsync(AgentPresenceChanged notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
