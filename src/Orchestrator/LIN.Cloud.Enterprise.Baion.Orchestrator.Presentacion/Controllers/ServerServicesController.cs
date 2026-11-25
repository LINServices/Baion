using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Constants;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/servers/{serverId:guid}/services")]
[Authorize]
public class ServerServicesController(IServiceInspectionService services) : ControllerBase
{
    /// <summary>
    /// Lista los servicios del sistema del servidor.
    /// </summary>
    /// <param name="serverId">Servidor a consultar.</param>
    /// <param name="filter">Acota por subcadena del nombre.</param>
    [HttpGet]
    public async Task<IActionResult> ListAsync(Guid serverId, [FromQuery] string? filter, CancellationToken cancellationToken)
    {
        return (await services.ListAsync(serverId, filter, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Obtiene el detalle de un servicio concreto del servidor.
    /// </summary>
    [HttpGet("{serviceId}")]
    public async Task<IActionResult> GetAsync(Guid serverId, string serviceId, CancellationToken cancellationToken)
    {
        return (await services.DescribeAsync(serverId, serviceId, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Instantánea de los logs del servicio.
    /// </summary>
    /// <param name="serverId">Servidor a consultar.</param>
    /// <param name="serviceId">Servicio cuyos logs se piden.</param>
    /// <param name="maxLines">Últimas líneas a devolver; si se omite, se aplica el valor por defecto.</param>
    /// <param name="since">Si se indica, solo se devuelven los logs a partir de ese instante.</param>
    [HttpGet("{serviceId}/logs")]
    public async Task<IActionResult> LogsAsync(Guid serverId, string serviceId, [FromQuery] int? maxLines, DateTimeOffset? since, CancellationToken cancellationToken)
    {
        return (await services.GetLogsAsync(serverId, serviceId, maxLines, since, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Aplica una acción de control sobre el servicio y devuelve su detalle resultante.
    /// </summary>
    [HttpPost("{serviceId}/control")]
    [Authorize(Roles = AuthorizationRoles.Admin)]
    public async Task<IActionResult> ControlAsync(Guid serverId, string serviceId, [FromBody] ControlServiceRequest request, CancellationToken cancellationToken)
    {
        return (await services.ControlAsync(serverId, serviceId, request.Action, cancellationToken)).ToActionResult();
    }
}
