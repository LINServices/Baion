using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(IServerService servers) : ControllerBase
{
    /// <summary>
    /// Cifras de cabecera del panel: servidores, scripts y ejecuciones de las últimas 24 horas.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummaryAsync(CancellationToken cancellationToken)
    {
        return Ok(await servers.GetDashboardSummaryAsync(cancellationToken));
    }
}
