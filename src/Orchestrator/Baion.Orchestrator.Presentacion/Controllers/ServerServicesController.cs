using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/servers/{serverId:guid}/services")]
[Authorize]
public class ServerServicesController(IServiceInspectionService services) : ControllerBase
{
    /// <summary>Lista los servicios del sistema del servidor. <paramref name="filter"/> acota por subcadena del nombre.</summary>
    [HttpGet]
    public async Task<IActionResult> ListarAsync(Guid serverId, [FromQuery] string? filter, CancellationToken cancellationToken) =>
        (await services.ListAsync(serverId, filter, cancellationToken)).ToActionResult();

    /// <summary>Obtiene el detalle de un servicio concreto del servidor.</summary>
    [HttpGet("{serviceId}")]
    public async Task<IActionResult> ObtenerAsync(Guid serverId, string serviceId, CancellationToken cancellationToken) =>
        (await services.DescribeAsync(serverId, serviceId, cancellationToken)).ToActionResult();

    /// <summary>
    /// Instantánea de los logs del servicio: las últimas <paramref name="maxLines"/> líneas (o el valor por
    /// defecto), acotadas a partir de <paramref name="since"/> si se indica.
    /// </summary>
    [HttpGet("{serviceId}/logs")]
    public async Task<IActionResult> LogsAsync(Guid serverId, string serviceId, [FromQuery] int? maxLines, DateTimeOffset? since, CancellationToken cancellationToken) =>
        (await services.GetLogsAsync(serverId, serviceId, maxLines, since, cancellationToken)).ToActionResult();

    /// <summary>Aplica una acción de control sobre el servicio y devuelve su detalle resultante.</summary>
    [HttpPost("{serviceId}/control")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> ControlarAsync(Guid serverId, string serviceId, [FromBody] ControlServiceRequest request, CancellationToken cancellationToken) =>
        (await services.ControlAsync(serverId, serviceId, request.Action, cancellationToken)).ToActionResult();

    private const string AdminRole = "Admin";
}
