using System;
using System.Threading;
using System.Threading.Tasks;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Messaging.Implementations;

// Sin broker no hay claves de enrutado que enlazar.
internal class NoOpAgentCommandSubscription : IAgentCommandSubscription
{
    public Task SubscribeAsync(Guid serverId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(Guid serverId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
