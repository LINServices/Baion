using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Implementations;

internal class ServerGroupRepository(BaionDbContext context) : Repository<ServerGroup>(context), IServerGroupRepository
{
    // La base no expone el contexto, así que se guarda aparte para las consultas propias del grupo.
    private readonly BaionDbContext _context = context;

    // El filtro global ya acota por tenant, tanto el grupo como sus miembros.
    public async Task<ServerGroup?> GetWithMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.ServerGroups
            .Include(group => group.Members)
            .FirstOrDefaultAsync(group => group.Id == groupId, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeGroupId, CancellationToken cancellationToken = default)
    {
        return await _context.ServerGroups
            .AsNoTracking()
            .AnyAsync(group => group.Name == name && (excludeGroupId == null || group.Id != excludeGroupId), cancellationToken);
    }

    public async Task<bool> HasScheduledTasksAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledTasks
            .AsNoTracking()
            .AnyAsync(task => task.ServerGroupId == groupId, cancellationToken);
    }

    public async Task<IReadOnlyList<GroupMemberTarget>> GetMemberTargetsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.ServerGroupMembers
            .AsNoTracking()
            .Where(member => member.ServerGroupId == groupId)
            .OrderBy(member => member.Server.Name)
            .ThenBy(member => member.ServerId)
            .Select(member => new GroupMemberTarget(member.ServerId, member.Server.Name, member.Server.Platform, member.Server.Status))
            .ToListAsync(cancellationToken);
    }

    // Se desactiva el filtro global a propósito para no depender del estado del scope (el tenant llega como argumento)
    // y el aislamiento se garantiza filtrando por tenant a mano, en la fila de pertenencia y en el servidor.
    public async Task<IReadOnlyList<GroupMemberTarget>> GetMemberTargetsAsync(Guid tenantId, Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.ServerGroupMembers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(member => member.TenantId == tenantId && member.ServerGroupId == groupId && member.Server.TenantId == tenantId)
            .OrderBy(member => member.Server.Name)
            .ThenBy(member => member.ServerId)
            .Select(member => new GroupMemberTarget(member.ServerId, member.Server.Name, member.Server.Platform, member.Server.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountServersAsync(IReadOnlyCollection<Guid> serverIds, CancellationToken cancellationToken = default)
    {
        if (serverIds.Count == 0)
            return 0;

        var ids = serverIds.Distinct().ToList();
        return await _context.Servers
            .AsNoTracking()
            .CountAsync(server => ids.Contains(server.Id), cancellationToken);
    }
}
