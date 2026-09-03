using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Identity;

/// <summary>Punto de entrada del login: resuelve el tenant, elige su proveedor y emite el token.</summary>
public interface IAuthenticationService
{
    /// <summary>Autentica al usuario contra el proveedor del tenant y devuelve el token de acceso.</summary>
    Task<Result<AuthenticationResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
