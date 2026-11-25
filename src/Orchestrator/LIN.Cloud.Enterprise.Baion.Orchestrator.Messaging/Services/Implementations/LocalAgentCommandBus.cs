using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Contracts.Messages;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Messaging.Implementations;

// Bus para el modo de una sola instancia: solo alcanza a los agentes de este proceso.
internal class LocalAgentCommandBus(ILocalAgentDelivery local) : IAgentCommandBus
{
    public async Task<bool> TrySendAsync(Guid serverId, ServerToAgentMessage message, CancellationToken cancellationToken)
    {
        return await local.TryDeliverAsync(serverId, message, cancellationToken);
    }
}
