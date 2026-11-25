using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Results;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Identity;

/// <summary>
/// Verifica credenciales contra el origen de identidad de un tenant. Solo cubre la verificación:
/// el token siempre lo emite Baion, de modo que la validación es idéntica en ambos modos.
/// </summary>
public interface IIdentityProvider
{
    /// <summary>
    /// Modo de identidad que atiende esta implementación.
    /// </summary>
    IdentityMode Mode { get; }

    /// <summary>
    /// Verifica las credenciales y devuelve la identidad del usuario.
    /// </summary>
    Task<Result<AuthenticatedUser>> VerifyCredentialsAsync(Tenant tenant, LoginRequest request, CancellationToken cancellationToken);
}
