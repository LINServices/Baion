using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/chains")]
[Authorize]
public class ChainsController(IScriptChainService chains) : ControllerBase
{
    /// <summary>Da de alta una cadena con sus pasos.</summary>
    [HttpPost]
    public async Task<IActionResult> CrearAsync([FromBody] CreateScriptChainRequest request, CancellationToken cancellationToken) => (await chains.CreateAsync(request, cancellationToken)).ToActionResult();

    /// <summary>Obtiene la ficha de una cadena.</summary>
    [HttpGet("{chainId:guid}")]
    public async Task<IActionResult> ObtenerAsync(Guid chainId, CancellationToken cancellationToken) => (await chains.GetAsync(chainId, cancellationToken)).ToActionResult();

    /// <summary>Arranca la cadena sobre un servidor y despacha su primer paso.</summary>
    [HttpPost("{chainId:guid}/runs")]
    public async Task<IActionResult> ArrancarAsync(Guid chainId, [FromBody] StartChainBody body, CancellationToken cancellationToken) => (await chains.StartAsync(new StartChainRequest(chainId, body.ServerId), cancellationToken)).ToActionResult();

    /// <summary>Consulta el avance de un recorrido de cadena.</summary>
    [HttpGet("runs/{chainRunId:guid}")]
    public async Task<IActionResult> ObtenerRecorridoAsync(Guid chainRunId, CancellationToken cancellationToken) => (await chains.GetRunAsync(chainRunId, cancellationToken)).ToActionResult();

    /// <summary>Cuerpo del arranque; la cadena viaja en la ruta.</summary>
    public record StartChainBody(Guid ServerId);
}
