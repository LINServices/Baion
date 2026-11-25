using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services.Implementations;

// Vacía el buzón de métricas contra la base en lotes. Es el único punto que escribe en metrics,
// de modo que los hilos de los sockets nunca esperan por la base de datos.
internal class MetricIngestHostedService(IMetricIngestQueue queue, IServiceScopeFactory scopeFactory, IOptions<MetricIngestOptions> options, TimeProvider timeProvider, ILogger<MetricIngestHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // El buzón lo publica MetricIngestQueue; el tipo concreto es el que sabe leerlo.
        if (queue is not MetricIngestQueue source)
        {
            logger.LogError("El buzón de métricas registrado no es {Tipo}; no se escribirá ninguna muestra", nameof(MetricIngestQueue));
            return;
        }

        var settings = options.Value;
        var batch = new List<MetricSample>(settings.BatchSize);
        var window = TimeSpan.FromMilliseconds(Math.Max(settings.BatchWindowMilliseconds, 1));

        while (!stoppingToken.IsCancellationRequested && await SafeWaitToReadAsync(source, stoppingToken))
        {
            await FillBatchAsync(source, batch, settings.BatchSize, window, stoppingToken);
            await FlushAsync(batch, stoppingToken);
        }

        // Al parar se intenta salvar lo que quedó en el lote en curso.
        await FlushAsync(batch, CancellationToken.None);
    }

    // Llena el lote hasta el tope o hasta que venza la ventana. La ventana se vigila con su propio plazo:
    // si solo se comprobara al llegar cada muestra, un lote a medias podría quedarse esperando indefinidamente.
    private async Task FillBatchAsync(MetricIngestQueue source, List<MetricSample> batch, int batchSize, TimeSpan window, CancellationToken stoppingToken)
    {
        var deadline = timeProvider.GetUtcNow() + window;

        var hayMas = true;

        while (hayMas && batch.Count < batchSize)
            hayMas = await TryTakeNextAsync(source, batch, deadline, stoppingToken);
    }

    // Toma la siguiente entrada o espera a que llegue como máximo hasta el límite. Devuelve false
    // cuando ya no hay que seguir llenando el lote (ventana vencida, buzón cerrado o parada).
    private async Task<bool> TryTakeNextAsync(MetricIngestQueue source, List<MetricSample> batch, DateTimeOffset deadline, CancellationToken stoppingToken)
    {
        if (source.TryRead(out var sample))
        {
            batch.Add(sample);
            return true;
        }

        var restante = deadline - timeProvider.GetUtcNow();

        if (restante <= TimeSpan.Zero)
            return false;

        using var ventana = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        ventana.CancelAfter(restante);

        try
        {
            return await source.WaitToReadAsync(ventana.Token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    // Escribe el lote agrupado por tenant: cada scope opera sobre un único tenant, que es lo que
    // exigen el filtro global y el interceptor de sellado.
    private async Task FlushAsync(List<MetricSample> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
            return;

        foreach (var grupo in batch.GroupBy(sample => sample.TenantId))
        {
            try
            {
                await WriteTenantBatchAsync(grupo.Key, grupo.ToList(), cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "No se pudo escribir un lote de {Muestras} métricas del tenant {TenantId}", grupo.Count(), grupo.Key);
            }
        }

        batch.Clear();
    }

    private async Task WriteTenantBatchAsync(Guid tenantId, IReadOnlyList<MetricSample> samples, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(tenantId);

        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();
        context.Metrics.AddRange(samples.Select(ToMetric));

        await context.SaveChangesAsync(cancellationToken);
        await RefreshLastSeenAsync(context, samples, cancellationToken);

        logger.LogDebug("Escritas {Muestras} métricas del tenant {TenantId}", samples.Count, tenantId);
    }

    private static async Task<bool> SafeWaitToReadAsync(MetricIngestQueue source, CancellationToken stoppingToken)
    {
        try
        {
            return await source.WaitToReadAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    // Una métrica también es señal de vida. Se actualiza aquí, en bloque y por servidor, en lugar de
    // hacerlo desde el socket una vez por mensaje.
    private static async Task RefreshLastSeenAsync(BaionDbContext context, IReadOnlyList<MetricSample> samples, CancellationToken cancellationToken)
    {
        foreach (var grupo in samples.GroupBy(sample => sample.ServerId))
        {
            var ultima = grupo.Max(sample => sample.Report.CapturedAt);

            await context.Servers
                .Where(server => server.Id == grupo.Key && (!server.LastSeenAt.HasValue || server.LastSeenAt < ultima))
                .ExecuteUpdateAsync(setters => setters.SetProperty(server => server.LastSeenAt, ultima), cancellationToken);
        }
    }

    private static Metric ToMetric(MetricSample sample)
    {
        return new()
        {
            TenantId = sample.TenantId,
            ServerId = sample.ServerId,
            CapturedAt = sample.Report.CapturedAt,
            CpuUsagePercent = sample.Report.Cpu.UsagePercent,
            CpuCoreCount = sample.Report.Cpu.CoreCount,
            LoadAverage1m = sample.Report.Cpu.LoadAverage1m,
            MemoryTotalBytes = sample.Report.Memory.TotalBytes,
            MemoryAvailableBytes = sample.Report.Memory.AvailableBytes,
            Disks = [.. sample.Report.Disks.Select(disk => new MetricDisk { Name = disk.Name, MountPoint = disk.MountPoint, TotalBytes = disk.TotalBytes, AvailableBytes = disk.AvailableBytes })]
        };
    }
}
