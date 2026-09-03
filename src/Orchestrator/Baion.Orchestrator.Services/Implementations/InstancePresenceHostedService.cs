using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Orchestrator.Services.Implementations;

/// <summary>
/// Limpia la presencia que esta instancia dejó registrada. Al arrancar suelta los servidores que quedaron
/// marcados como suyos tras una caída, y al parar hace lo mismo de forma ordenada. Es lo que permite que
/// los agentes sobrevivan a un reinicio del orquestador sin quedar en un estado "conectado" que ya no existe.
/// </summary>
internal class InstancePresenceHostedService(IServiceScopeFactory scopeFactory, IOptions<OrchestratorOptions> options, ILogger<InstancePresenceHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken) => await ReleaseAsync("arranque", cancellationToken);

    public async Task StopAsync(CancellationToken cancellationToken) => await ReleaseAsync("parada", cancellationToken);

    private async Task ReleaseAsync(string motivo, CancellationToken cancellationToken)
    {
        var instanceId = options.Value.InstanceId;

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var liberados = await scope.ServiceProvider.GetRequiredService<IAgentRepository>().ReleaseInstanceServersAsync(instanceId, cancellationToken);

            if (liberados > 0)
            {
                logger.LogInformation("Liberados {Servidores} servidores de la instancia {InstanceId} en el {Motivo}", liberados, instanceId, motivo);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo liberar la presencia de la instancia {InstanceId} en el {Motivo}", instanceId, motivo);
        }
    }
}
