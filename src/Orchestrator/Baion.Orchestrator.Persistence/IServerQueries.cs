using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;

namespace Baion.Orchestrator.Persistence;

/// <summary>
/// Consultas de solo lectura que alimentan el panel. Van aparte de los repositorios porque proyectan
/// directamente a DTO: traer entidades enteras para pintar una tabla es trabajo de más.
/// </summary>
public interface IServerQueries
{
    /// <summary>Servidores del tenant actual, del visto más recientemente al más antiguo.</summary>
    Task<IReadOnlyList<ServerSummary>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Última muestra de métricas de un servidor, o null si nunca reportó.</summary>
    Task<ServerMetricsSnapshot?> GetLastMetricsAsync(Guid serverId, CancellationToken cancellationToken);

    /// <summary>Histórico de métricas de un servidor dentro de <paramref name="window"/>, de la más reciente a la más antigua.</summary>
    Task<PagedResult<MetricReading>> ListMetricsAsync(Guid serverId, MetricsWindow window, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Cifras agregadas del panel; <paramref name="since"/> acota la ventana de ejecuciones.</summary>
    Task<DashboardSummary> GetDashboardSummaryAsync(DateTimeOffset since, CancellationToken cancellationToken);
}
