using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/scripts")]
[Authorize]
public class ScriptsController(IScriptService scripts) : ControllerBase
{
    /// <summary>
    /// Da de alta un script en el tenant actual.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateScriptRequest request, CancellationToken cancellationToken)
    {
        return (await scripts.CreateAsync(request, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Edita un script del tenant actual. La versión sube solo si cambia el contenido.
    /// </summary>
    [HttpPut("{scriptId:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid scriptId, [FromBody] UpdateScriptRequest request, CancellationToken cancellationToken)
    {
        return (await scripts.UpdateAsync(scriptId, request, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Lista los scripts del tenant actual, con búsqueda opcional por nombre.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListAsync([FromQuery] string? search, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        return Ok(await scripts.ListAsync(search, page, pageSize, cancellationToken));
    }

    /// <summary>
    /// Obtiene un script con su contenido.
    /// </summary>
    [HttpGet("{scriptId:guid}")]
    public async Task<IActionResult> GetAsync(Guid scriptId, CancellationToken cancellationToken)
    {
        return (await scripts.GetDetailAsync(scriptId, cancellationToken)).ToActionResult();
    }
}
