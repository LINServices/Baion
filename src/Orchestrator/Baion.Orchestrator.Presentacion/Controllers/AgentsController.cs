using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/agents")]
[Authorize(Roles = AdminRole)]
public class AgentsController(IEnrollmentTokenService enrollmentTokens) : ControllerBase
{
    /// <summary>Emite un token de instalación para enrolar agentes en el tenant actual.</summary>
    [HttpPost("enrollment-tokens")]
    public async Task<IActionResult> CreateEnrollmentTokenAsync([FromBody] CreateEnrollmentTokenRequest request, CancellationToken cancellationToken) => (await enrollmentTokens.CreateAsync(request, cancellationToken)).ToActionResult();

    /// <summary>Lista los tokens de instalación del tenant. Nunca devuelve su valor, solo su estado.</summary>
    [HttpGet("enrollment-tokens")]
    public async Task<IActionResult> ListarTokensAsync(CancellationToken cancellationToken) => Ok(await enrollmentTokens.ListAsync(cancellationToken));

    /// <summary>Revoca un token de instalación para que no admita más enrolamientos.</summary>
    [HttpDelete("enrollment-tokens/{tokenId:guid}")]
    public async Task<IActionResult> RevokeEnrollmentTokenAsync(Guid tokenId, CancellationToken cancellationToken) => (await enrollmentTokens.RevokeAsync(tokenId, cancellationToken)).ToActionResult();

    private const string AdminRole = "Admin";
}
