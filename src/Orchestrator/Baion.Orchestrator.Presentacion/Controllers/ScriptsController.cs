using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/scripts")]
[Authorize]
public class ScriptsController(IScriptService scripts) : ControllerBase
{
    /// <summary>Da de alta un script en el tenant actual.</summary>
    [HttpPost]
    public async Task<IActionResult> CrearAsync([FromBody] CreateScriptRequest request, CancellationToken cancellationToken) => (await scripts.CreateAsync(request, cancellationToken)).ToActionResult();

    /// <summary>Edita un script del tenant actual. La versión sube solo si cambia el contenido.</summary>
    [HttpPut("{scriptId:guid}")]
    public async Task<IActionResult> EditarAsync(Guid scriptId, [FromBody] UpdateScriptRequest request, CancellationToken cancellationToken) => (await scripts.UpdateAsync(scriptId, request, cancellationToken)).ToActionResult();

    /// <summary>Lista los scripts del tenant actual, con búsqueda opcional por nombre.</summary>
    [HttpGet]
    public async Task<IActionResult> ListarAsync([FromQuery] string? search, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default) => Ok(await scripts.ListAsync(search, page, pageSize, cancellationToken));

    /// <summary>Obtiene un script con su contenido.</summary>
    [HttpGet("{scriptId:guid}")]
    public async Task<IActionResult> ObtenerAsync(Guid scriptId, CancellationToken cancellationToken) => (await scripts.GetDetailAsync(scriptId, cancellationToken)).ToActionResult();
}
