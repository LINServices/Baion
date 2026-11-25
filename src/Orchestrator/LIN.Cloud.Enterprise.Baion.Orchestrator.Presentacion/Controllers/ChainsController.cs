using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/chains")]
[Authorize]
public class ChainsController(IScriptChainService chains) : ControllerBase
{
    /// <summary>
    /// Da de alta una cadena con sus pasos.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateScriptChainRequest request, CancellationToken cancellationToken)
    {
        return (await chains.CreateAsync(request, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Obtiene la ficha de una cadena.
    /// </summary>
    [HttpGet("{chainId:guid}")]
    public async Task<IActionResult> GetAsync(Guid chainId, CancellationToken cancellationToken)
    {
        return (await chains.GetAsync(chainId, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Arranca la cadena sobre un servidor y despacha su primer paso.
    /// </summary>
    [HttpPost("{chainId:guid}/runs")]
    public async Task<IActionResult> StartAsync(Guid chainId, [FromBody] StartChainBody body, CancellationToken cancellationToken)
    {
        return (await chains.StartAsync(new StartChainRequest(chainId, body.ServerId), cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Consulta el avance de un recorrido de cadena.
    /// </summary>
    [HttpGet("runs/{chainRunId:guid}")]
    public async Task<IActionResult> GetRunAsync(Guid chainRunId, CancellationToken cancellationToken)
    {
        return (await chains.GetRunAsync(chainRunId, cancellationToken)).ToActionResult();
    }
}
