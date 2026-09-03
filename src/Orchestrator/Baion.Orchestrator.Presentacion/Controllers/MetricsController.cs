using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/servers/{serverId:guid}/metrics")]
[Authorize]
public class MetricsController(IServerService servers) : ControllerBase
{
    /// <summary>
    /// Histórico de métricas del servidor, de la muestra más reciente a la más antigua. <paramref name="since"/>
    /// y <paramref name="until"/> acotan la ventana; cada extremo omitido deja ese lado abierto.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListarAsync(Guid serverId, [FromQuery] DateTimeOffset? since, DateTimeOffset? until, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default) =>
        (await servers.GetMetricsAsync(serverId, new MetricsWindow(since, until), page, pageSize, cancellationToken)).ToActionResult();
}
