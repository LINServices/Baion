using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Agent.Core;
using LIN.Cloud.Enterprise.Baion.Contracts.Messages;

namespace LIN.Cloud.Enterprise.Baion.Agent.Services.Implementations;

// Recibe las cuatro peticiones de servicios del orquestador y las pasa al procesador. Vuelve al instante:
// el bucle de recepción del socket no puede quedarse esperando a que systemctl o PowerShell respondan.
internal class ServiceQueryHandler(IServiceQueryProcessor processor) : IServerMessageHandler
{
    public bool CanHandle(ServerToAgentMessage message)
    {
        return message is ListServicesRequestMessage or DescribeServiceRequestMessage or ServiceLogsRequestMessage or ControlServiceRequestMessage;
    }

    public Task HandleAsync(ServerToAgentMessage message, CancellationToken cancellationToken)
    {
        processor.Enqueue(message);
        return Task.CompletedTask;
    }
}
