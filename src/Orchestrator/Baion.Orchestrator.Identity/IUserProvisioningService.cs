using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Identity;

/// <summary>Alta de usuarios en tenants autogestionados.</summary>
public interface IUserProvisioningService
{
    /// <summary>Crea un usuario en el tenant indicado y le asigna los roles pedidos, creándolos si aún no existen.</summary>
    Task<Result<Guid>> CreateUserAsync(Guid tenantId, CreateUserRequest request, CancellationToken cancellationToken);
}
