using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Identity;

/// <summary>
/// Verifica credenciales contra el origen de identidad de un tenant. Solo cubre la verificación:
/// el token siempre lo emite Baion, de modo que la validación es idéntica en ambos modos.
/// </summary>
public interface IIdentityProvider
{
    /// <summary>Modo de identidad que atiende esta implementación.</summary>
    IdentityMode Mode { get; }

    /// <summary>Verifica las credenciales y devuelve la identidad del usuario.</summary>
    Task<Result<AuthenticatedUser>> VerifyCredentialsAsync(Tenant tenant, LoginRequest request, CancellationToken cancellationToken);
}
