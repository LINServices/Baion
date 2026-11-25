using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;

/// <summary>
/// Acceso a los grupos de servidores del tenant actual.
/// </summary>
public interface IServerGroupRepository : IRepository<ServerGroup>
{
    /// <summary>
    /// Obtiene un grupo con sus miembros y con seguimiento de cambios. Devuelve null si no existe en el tenant.
    /// </summary>
    Task<ServerGroup?> GetWithMembersAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si ya hay un grupo con ese nombre en el tenant, ignorando <paramref name="excludeGroupId"/>.
    /// </summary>
    Task<bool> NameExistsAsync(string name, Guid? excludeGroupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si alguna tarea programada, activa o pausada, apunta al grupo.
    /// </summary>
    Task<bool> HasScheduledTasksAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Miembros del grupo acotados al tenant del scope actual.
    /// </summary>
    Task<IReadOnlyList<GroupMemberTarget>> GetMemberTargetsAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Miembros del grupo con el tenant indicado de forma explícita, para el planificador, que no tiene tenant en el scope.
    /// </summary>
    Task<IReadOnlyList<GroupMemberTarget>> GetMemberTargetsAsync(Guid tenantId, Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cuántos de los servidores indicados existen en el tenant actual.
    /// </summary>
    Task<int> CountServersAsync(IReadOnlyCollection<Guid> serverIds, CancellationToken cancellationToken = default);
}
