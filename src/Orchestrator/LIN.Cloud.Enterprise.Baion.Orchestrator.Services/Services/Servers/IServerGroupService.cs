using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Results;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

/// <summary>
/// Grupos de servidores del tenant actual.
/// </summary>
public interface IServerGroupService
{
    /// <summary>
    /// Lista los grupos con el recuento de miembros por plataforma.
    /// </summary>
    Task<Result<IReadOnlyList<ServerGroupSummary>>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Obtiene un grupo con sus miembros.
    /// </summary>
    Task<Result<ServerGroupDetail>> GetAsync(Guid groupId, CancellationToken cancellationToken);

    /// <summary>
    /// Crea un grupo con los miembros indicados.
    /// </summary>
    Task<Result<ServerGroupDetail>> CreateAsync(CreateServerGroupRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Actualiza el nombre y la descripción y reemplaza por completo los miembros.
    /// </summary>
    Task<Result<ServerGroupDetail>> UpdateAsync(Guid groupId, UpdateServerGroupRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Elimina el grupo y sus pertenencias, sin tocar los servidores. Falla si una tarea programada lo usa.
    /// </summary>
    Task<Result> DeleteAsync(Guid groupId, CancellationToken cancellationToken);
}
