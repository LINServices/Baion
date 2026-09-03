using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/executions")]
[Authorize]
public class ExecutionsController(IScriptDispatchService dispatch) : ControllerBase
{
    /// <summary>Despacha un script a un servidor y devuelve el identificador de la ejecución.</summary>
    [HttpPost]
    public async Task<IActionResult> DespacharAsync([FromBody] DispatchScriptRequest request, CancellationToken cancellationToken) => (await dispatch.DispatchAsync(request, cancellationToken)).ToActionResult();

    /// <summary>Lista las ejecuciones del tenant actual, de la más reciente a la más antigua y sin su salida.</summary>
    [HttpGet]
    public async Task<IActionResult> ListarAsync([FromQuery] Guid? serverId, Guid? scriptId, ExecutionStatus? status, DateTimeOffset? since, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default) => Ok(await dispatch.ListAsync(new ExecutionFilter(serverId, scriptId, status, since), page, pageSize, cancellationToken));

    /// <summary>Consulta el estado y la salida acumulada de una ejecución.</summary>
    [HttpGet("{executionId:guid}")]
    public async Task<IActionResult> ObtenerAsync(Guid executionId, CancellationToken cancellationToken) => (await dispatch.GetAsync(executionId, cancellationToken)).ToActionResult();
}
