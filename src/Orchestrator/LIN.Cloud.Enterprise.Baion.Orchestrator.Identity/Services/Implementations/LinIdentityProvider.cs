using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Results;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Identity.Implementations;

// Delegará la verificación en LIN Cloud Identity Platform. La interfaz ya está en su sitio para que el resto del flujo de login no cambie cuando se implemente; hoy rechaza con un error explícito.
internal class LinIdentityProvider : IIdentityProvider
{
    private static readonly Result<AuthenticatedUser> NotImplemented = Result<AuthenticatedUser>.Failure(Error.Unexpected("identity.lin_unavailable", "La integración con LIN Cloud Identity Platform aún no está implementada."));

    public IdentityMode Mode => IdentityMode.Lin;

    public Task<Result<AuthenticatedUser>> VerifyCredentialsAsync(Tenant tenant, LoginRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(NotImplemented);
    }
}
