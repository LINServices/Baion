using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Agent.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LIN.Cloud.Enterprise.Baion.Agent.Metrics.Implementations;

// Va en su propio bucle, aparte del de recepción: recolectar implica leer del sistema de archivos
// o llamar al kernel, y eso no puede retrasar la lectura del socket.
internal class MetricsReportingWorker(IMetricsCollector collector, IOrchestratorChannel channel, IOptions<MetricsOptions> options, TimeProvider timeProvider, ILogger<MetricsReportingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var period = TimeSpan.FromSeconds(Math.Max(options.Value.IntervalSeconds, 1));
        using var timer = new PeriodicTimer(period, timeProvider);

        while (await SafeWaitAsync(timer, stoppingToken))
            await ReportAsync(stoppingToken);
    }

    private async Task ReportAsync(CancellationToken stoppingToken)
    {
        // Mientras no haya sesión no se acumula nada: una métrica vieja no aporta y el orquestador
        // ya sabe que el servidor estuvo desconectado porque se le cayó el socket.
        if (!channel.IsConnected)
            return;

        try
        {
            var report = await collector.CollectAsync(stoppingToken);
            await channel.TrySendAsync(report, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // El apagado termina el bucle principal en la siguiente espera.
        }
        catch (Exception exception)
        {
            logger.LogWarning("No se pudo reportar métricas: {Motivo}", exception.Message);
        }
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
