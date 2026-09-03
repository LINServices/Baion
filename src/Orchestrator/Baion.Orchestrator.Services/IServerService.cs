using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Services;

/// <summary>Consulta de los servidores del tenant actual y de las cifras del panel.</summary>
public interface IServerService
{
    /// <summary>Lista los servidores del tenant, del más recientemente visto al más antiguo.</summary>
    Task<IReadOnlyList<ServerSummary>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Obtiene un servidor con su última lectura de métricas.</summary>
    Task<Result<ServerDetail>> GetAsync(Guid serverId, CancellationToken cancellationToken);

    /// <summary>Histórico de métricas de un servidor del tenant, de la muestra más reciente a la más antigua.</summary>
    Task<Result<PagedResult<MetricReading>>> GetMetricsAsync(Guid serverId, MetricsWindow window, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Desactiva un servidor por la fuerza: corta la conexión que tenga abierta, esté en la instancia que
    /// esté, y le cierra la puerta hasta que alguien lo reactive. La credencial del agente se conserva.
    /// </summary>
    Task<Result<ServerSummary>> DisableAsync(Guid serverId, CancellationToken cancellationToken);

    /// <summary>Vuelve a admitir al agente de un servidor desactivado. Reconectará él solo.</summary>
    Task<Result<ServerSummary>> EnableAsync(Guid serverId, CancellationToken cancellationToken);

    /// <summary>Cifras agregadas para la cabecera del panel.</summary>
    Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken);
}
