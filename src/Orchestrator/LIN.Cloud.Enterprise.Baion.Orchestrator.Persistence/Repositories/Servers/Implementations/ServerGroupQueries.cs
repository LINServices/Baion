using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Implementations;

internal class ServerGroupQueries(BaionDbContext context) : IServerGroupQueries
{
    // Los recuentos salen de subconsultas correlacionadas dentro de una sola sentencia: sin Include ni N+1.
    public async Task<IReadOnlyList<ServerGroupSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await context.ServerGroups
            .AsNoTracking()
            .OrderBy(group => group.Name)
            .ThenBy(group => group.Id)
            .Select(group => new ServerGroupSummary(
                group.Id,
                group.Name,
                group.Description,
                group.Members.Count(),
                group.Members.Count(member => member.Server.Platform == ServerPlatform.Linux),
                group.Members.Count(member => member.Server.Platform == ServerPlatform.Windows),
                group.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServerGroupDetail?> GetAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var grupo = await context.ServerGroups
            .AsNoTracking()
            .Where(group => group.Id == groupId)
            .Select(group => new
            {
                group.Id,
                group.Name,
                group.Description,
                group.CreatedAt,
                Members = group.Members
                    .OrderBy(member => member.Server.Name)
                    .ThenBy(member => member.ServerId)
                    .Select(member => new ServerGroupMemberSummary(
                        member.ServerId,
                        member.Server.Name,
                        member.Server.Hostname,
                        member.Server.Platform,
                        member.Server.Status))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return grupo is null
            ? null
            : new ServerGroupDetail(grupo.Id, grupo.Name, grupo.Description, grupo.Members, grupo.CreatedAt);
    }
}
