using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/executions")]
[Authorize]
public class ExecutionsController(IScriptDispatchService dispatch, IGroupDispatchService groupDispatch) : ControllerBase
{
    /// <summary>
    /// Lanza un script sobre todos los miembros de un grupo. El solicitante se toma de la sesión.
    /// </summary>
    [HttpPost("batches")]
    public async Task<IActionResult> DispatchGroupAsync([FromBody] DispatchGroupRequest request, CancellationToken cancellationToken)
    {
        return (await groupDispatch.DispatchAsync(request, User.Identity?.Name, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Consulta un lote con sus ejecuciones, sin su salida.
    /// </summary>
    [HttpGet("batches/{batchId:guid}")]
    public async Task<IActionResult> GetBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        return (await groupDispatch.GetBatchAsync(batchId, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Despacha un script a un servidor y devuelve el identificador de la ejecución.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DispatchAsync([FromBody] DispatchScriptRequest request, CancellationToken cancellationToken)
    {
        return (await dispatch.DispatchAsync(request, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Lista las ejecuciones del tenant actual, de la más reciente a la más antigua y sin su salida.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListAsync([FromQuery] Guid? serverId, Guid? scriptId, ExecutionStatus? status, DateTimeOffset? since, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        return Ok(await dispatch.ListAsync(new ExecutionFilter(serverId, scriptId, status, since), page, pageSize, cancellationToken));
    }

    /// <summary>
    /// Consulta el estado y la salida acumulada de una ejecución.
    /// </summary>
    [HttpGet("{executionId:guid}")]
    public async Task<IActionResult> GetAsync(Guid executionId, CancellationToken cancellationToken)
    {
        return (await dispatch.GetAsync(executionId, cancellationToken)).ToActionResult();
    }
}
