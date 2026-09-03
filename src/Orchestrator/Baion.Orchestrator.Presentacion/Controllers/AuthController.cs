using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Identity;
using Baion.Orchestrator.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    /// <summary>Autentica un usuario contra su tenant y devuelve el token de acceso.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken) => (await authenticationService.LoginAsync(request, cancellationToken)).ToActionResult();
}
