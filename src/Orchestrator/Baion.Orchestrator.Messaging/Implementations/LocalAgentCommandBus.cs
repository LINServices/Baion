using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Messages;
using Baion.Orchestrator.Models.Dtos;

namespace Baion.Orchestrator.Messaging.Implementations;

/// <summary>Bus para el modo de una sola instancia: solo alcanza a los agentes de este proceso.</summary>
internal class LocalAgentCommandBus(ILocalAgentDelivery local) : IAgentCommandBus
{
    public async Task<bool> TrySendAsync(Guid serverId, ServerToAgentMessage message, CancellationToken cancellationToken) => await local.TryDeliverAsync(serverId, message, cancellationToken);
}

/// <summary>Sin más instancias a las que avisar, la presencia no viaja a ninguna parte.</summary>
internal class NoOpAgentPresenceBus : IAgentPresenceBus
{
    public Task PublishAsync(AgentPresenceChanged notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Sin broker no hay claves de enrutado que enlazar.</summary>
internal class NoOpAgentCommandSubscription : IAgentCommandSubscription
{
    public Task SubscribeAsync(Guid serverId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task UnsubscribeAsync(Guid serverId, CancellationToken cancellationToken) => Task.CompletedTask;
}
