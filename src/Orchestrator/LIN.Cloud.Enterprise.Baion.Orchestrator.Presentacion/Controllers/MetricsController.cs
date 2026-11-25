using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/servers/{serverId:guid}/metrics")]
[Authorize]
public class MetricsController(IServerService servers) : ControllerBase
{
    /// <summary>
    /// Histórico de métricas del servidor, de la muestra más reciente a la más antigua. Cada extremo
    /// omitido de la ventana deja ese lado abierto.
    /// </summary>
    /// <param name="serverId">Servidor del que se consultan las métricas.</param>
    /// <param name="since">Inicio de la ventana, incluido.</param>
    /// <param name="until">Fin de la ventana, excluido.</param>
    /// <param name="page">Página a devolver, empezando en 1.</param>
    /// <param name="pageSize">Muestras por página.</param>
    [HttpGet]
    public async Task<IActionResult> ListAsync(Guid serverId, [FromQuery] DateTimeOffset? since, DateTimeOffset? until, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        return (await servers.GetMetricsAsync(serverId, new MetricsWindow(since, until), page, pageSize, cancellationToken)).ToActionResult();
    }
}
