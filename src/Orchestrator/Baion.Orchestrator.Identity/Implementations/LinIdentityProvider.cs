using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Identity.Implementations;

/// <summary>
/// Delegará la verificación en LIN Cloud Identity Platform. La interfaz ya está en su sitio para que
/// el resto del flujo de login no cambie cuando se implemente; hoy rechaza con un error explícito.
/// </summary>
internal class LinIdentityProvider : IIdentityProvider
{
    public IdentityMode Mode => IdentityMode.Lin;

    public Task<Result<AuthenticatedUser>> VerifyCredentialsAsync(Tenant tenant, LoginRequest request, CancellationToken cancellationToken) => Task.FromResult(NotImplemented);

    private static readonly Result<AuthenticatedUser> NotImplemented = Result<AuthenticatedUser>.Failure(Error.Unexpected("identity.lin_unavailable", "La integración con LIN Cloud Identity Platform aún no está implementada."));
}
