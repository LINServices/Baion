using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Results;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Identity;

/// <summary>
/// Alta de tenants.
/// </summary>
public interface ITenantProvisioningService
{
    /// <summary>
    /// Crea el tenant si el slug está libre; si ya existe, devuelve el identificador del existente.
    /// </summary>
    Task<Result<Guid>> EnsureTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken);
}
