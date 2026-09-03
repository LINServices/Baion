using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(IServerService servers) : ControllerBase
{
    /// <summary>Cifras de cabecera del panel: servidores, scripts y ejecuciones de las últimas 24 horas.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> ResumenAsync(CancellationToken cancellationToken) => Ok(await servers.GetDashboardSummaryAsync(cancellationToken));
}
