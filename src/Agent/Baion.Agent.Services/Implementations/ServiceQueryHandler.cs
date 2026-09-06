using System.Threading;
using System.Threading.Tasks;
using Baion.Agent.Core;
using Baion.Contracts.Messages;

namespace Baion.Agent.Services.Implementations;

/// <summary>
/// Recibe las cuatro peticiones de servicios del orquestador y las pasa al procesador. Vuelve al instante:
/// el bucle de recepción del socket no puede quedarse esperando a que <c>systemctl</c> o PowerShell respondan.
/// </summary>
internal class ServiceQueryHandler(IServiceQueryProcessor processor) : IServerMessageHandler
{
    public bool CanHandle(ServerToAgentMessage message)
        => message is ListServicesRequestMessage or DescribeServiceRequestMessage or ServiceLogsRequestMessage or ControlServiceRequestMessage;

    public Task HandleAsync(ServerToAgentMessage message, CancellationToken cancellationToken)
    {
        processor.Enqueue(message);
        return Task.CompletedTask;
    }
}
