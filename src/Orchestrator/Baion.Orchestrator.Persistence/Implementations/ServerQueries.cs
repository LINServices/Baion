using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Baion.Orchestrator.Persistence.Implementations;

internal class ServerQueries(BaionDbContext context) : IServerQueries
{
    public async Task<IReadOnlyList<ServerSummary>> ListAsync(CancellationToken cancellationToken) => await context.Servers
        .AsNoTracking()
        .OrderByDescending(server => server.LastSeenAt)
        .ThenBy(server => server.Name)
        .Select(server => new ServerSummary(
            server.Id,
            server.Name,
            server.Hostname,
            server.Kind,
            server.Platform,
            server.Status,
            server.AgentVersion,
            server.RuntimeIdentifier,
            server.OrchestratorInstanceId,
            server.ConnectedAt,
            server.LastSeenAt,
            server.MaxConcurrentExecutions))
        .ToListAsync(cancellationToken);

    // El índice agrupado de metrics es (server_id, captured_at), así que el más reciente sale de un seek.
    public async Task<ServerMetricsSnapshot?> GetLastMetricsAsync(Guid serverId, CancellationToken cancellationToken) => await context.Metrics
        .AsNoTracking()
        .Where(metric => metric.ServerId == serverId)
        .OrderByDescending(metric => metric.CapturedAt)
        .Select(metric => new ServerMetricsSnapshot(metric.CapturedAt, metric.CpuUsagePercent, metric.CpuCoreCount, metric.MemoryTotalBytes, metric.MemoryAvailableBytes))
        .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<MetricReading>> ListMetricsAsync(Guid serverId, MetricsWindow window, int page, int pageSize, CancellationToken cancellationToken)
    {
        var (pagina, tamano) = Pagination.Normalize(page, pageSize);

        var consulta = context.Metrics
            .AsNoTracking()
            .Where(metric => metric.ServerId == serverId);

        if (window.Since is DateTimeOffset desde)
        {
            consulta = consulta.Where(metric => metric.CapturedAt >= desde);
        }

        if (window.Until is DateTimeOffset hasta)
        {
            consulta = consulta.Where(metric => metric.CapturedAt < hasta);
        }

        var total = await consulta.CountAsync(cancellationToken);

        // El índice agrupado es (server_id, captured_at): pedir el orden descendente sale de un seek inverso.
        var elementos = await consulta
            .OrderByDescending(metric => metric.CapturedAt)
            .ThenByDescending(metric => metric.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(metric => new MetricReading(
                metric.CapturedAt,
                metric.CpuUsagePercent,
                metric.CpuCoreCount,
                metric.LoadAverage1m,
                metric.MemoryTotalBytes,
                metric.MemoryAvailableBytes,
                metric.Disks
                    .Select(disk => new MetricDiskReading(disk.Name, disk.MountPoint, disk.TotalBytes, disk.AvailableBytes))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new PagedResult<MetricReading>(elementos, pagina, tamano, total);
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        var servers = await context.Servers
            .AsNoTracking()
            .GroupBy(server => server.Status)
            .Select(grupo => new { Estado = grupo.Key, Total = grupo.Count() })
            .ToListAsync(cancellationToken);

        var ejecuciones = await context.ScriptExecutions
            .AsNoTracking()
            .Where(execution => execution.QueuedAt >= since)
            .GroupBy(execution => execution.Status)
            .Select(grupo => new { Estado = grupo.Key, Total = grupo.Count() })
            .ToListAsync(cancellationToken);

        var scripts = await context.Scripts.AsNoTracking().CountAsync(script => script.IsActive, cancellationToken);

        // Las que siguen vivas se cuentan sin ventana: una ejecución larga pudo empezar hace más de un día.
        var enCurso = await context.ScriptExecutions
            .AsNoTracking()
            .CountAsync(execution => execution.Status == ExecutionStatus.Pending || execution.Status == ExecutionStatus.Dispatched || execution.Status == ExecutionStatus.Running, cancellationToken);

        var fallidas = ejecuciones
            .Where(fila => fila.Estado is ExecutionStatus.Failed or ExecutionStatus.TimedOut)
            .Sum(fila => fila.Total);

        return new DashboardSummary(
            servers.Sum(fila => fila.Total),
            servers.FirstOrDefault(fila => fila.Estado is ServerStatus.Online)?.Total ?? 0,
            servers.Where(fila => fila.Estado is not ServerStatus.Online).Sum(fila => fila.Total),
            scripts,
            enCurso,
            ejecuciones.Sum(fila => fila.Total),
            fallidas);
    }
}
