using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Results;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Identity;

/// <summary>
/// Alta de usuarios en tenants autogestionados.
/// </summary>
public interface IUserProvisioningService
{
    /// <summary>
    /// Crea un usuario en el tenant indicado y le asigna los roles pedidos, creándolos si aún no existen.
    /// </summary>
    Task<Result<Guid>> CreateUserAsync(Guid tenantId, CreateUserRequest request, CancellationToken cancellationToken);
}
