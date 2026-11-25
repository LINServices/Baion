using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Agent.Core;
using LIN.Cloud.Enterprise.Baion.Contracts.Messages;

namespace LIN.Cloud.Enterprise.Baion.Agent.Execution.Implementations;

// Entrega la orden al coordinador y vuelve al instante: el bucle de recepción del socket
// no puede quedarse esperando a que un script termine.
internal class ExecuteScriptHandler(IScriptExecutionCoordinator coordinator) : IServerMessageHandler
{
    public bool CanHandle(ServerToAgentMessage message)
    {
        return message is ExecuteScriptMessage;
    }

    public Task HandleAsync(ServerToAgentMessage message, CancellationToken cancellationToken)
    {
        coordinator.Enqueue((ExecuteScriptMessage)message);
        return Task.CompletedTask;
    }
}
