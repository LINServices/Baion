using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Agent.Core;
using LIN.Cloud.Enterprise.Baion.Contracts.Messages;

namespace LIN.Cloud.Enterprise.Baion.Agent.Services.Tests.Fakes;

internal sealed class FakeChannel : IOrchestratorChannel
{
    private readonly TaskCompletionSource<AgentToServerMessage> _enviado = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsConnected => true;

    public AgentSessionInfo? Session => null;

    public Task<bool> TrySendAsync(AgentToServerMessage message, CancellationToken cancellationToken)
    {
        _enviado.TrySetResult(message);
        return Task.FromResult(true);
    }

    public Task<AgentToServerMessage> Esperar(TimeSpan tiempo)
    {
        return _enviado.Task.WaitAsync(tiempo);
    }
}
