using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Identity;

/// <summary>Alta de tenants.</summary>
public interface ITenantProvisioningService
{
    /// <summary>Crea el tenant si el slug está libre; si ya existe, devuelve el identificador del existente.</summary>
    Task<Result<Guid>> EnsureTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken);
}
