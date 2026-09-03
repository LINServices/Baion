using System.Threading;
using System.Threading.Tasks;
using Baion.Agent.Core;
using Baion.Contracts.Messages;

namespace Baion.Agent.Execution.Implementations;

/// <summary>
/// Entrega la orden al coordinador y vuelve al instante. El bucle de recepción del socket no puede
/// quedarse esperando a que un script termine.
/// </summary>
internal class ExecuteScriptHandler(IScriptExecutionCoordinator coordinator) : IServerMessageHandler
{
    public bool CanHandle(ServerToAgentMessage message) => message is ExecuteScriptMessage;

    public Task HandleAsync(ServerToAgentMessage message, CancellationToken cancellationToken)
    {
        coordinator.Enqueue((ExecuteScriptMessage)message);
        return Task.CompletedTask;
    }
}
