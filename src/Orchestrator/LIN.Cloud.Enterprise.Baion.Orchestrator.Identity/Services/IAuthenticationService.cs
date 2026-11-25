using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Results;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Identity;

/// <summary>
/// Punto de entrada del login: resuelve el tenant, elige su proveedor y emite el token.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Autentica al usuario contra el proveedor del tenant y devuelve el token de acceso.
    /// </summary>
    Task<Result<AuthenticationResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
