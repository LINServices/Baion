using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Baion.Orchestrator.Persistence.Implementations;

internal class ScheduledTaskRepository(BaionDbContext context) : IScheduledTaskRepository
{
    public async Task<ScheduledTask?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken) => await context.ScheduledTasks.FirstOrDefaultAsync(task => task.Id == taskId, cancellationToken);

    public async Task AddAsync(ScheduledTask task) => await context.ScheduledTasks.AddAsync(task);

    public async Task<IReadOnlyList<ScheduledTask>> GetDueAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken) => await context.ScheduledTasks
        .IgnoreQueryFilters()
        .AsNoTracking()
        .Where(task => task.IsEnabled && task.NextRunAt != null && task.NextRunAt <= now)
        .OrderBy(task => task.NextRunAt)
        .Take(limit)
        .ToListAsync(cancellationToken);

    // ExecuteUpdate lleva la condición al WHERE, así que la reserva es una única sentencia atómica.
    public async Task<bool> TryClaimAsync(Guid taskId, DateTimeOffset expectedNextRunAt, DateTimeOffset nextOccurrence, DateTimeOffset now, CancellationToken cancellationToken) => await context.ScheduledTasks
        .IgnoreQueryFilters()
        .Where(task => task.Id == taskId && task.NextRunAt == expectedNextRunAt)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(task => task.NextRunAt, nextOccurrence)
            .SetProperty(task => task.LastRunAt, now), cancellationToken) == 1;

    public async Task<IReadOnlyList<Guid>> GetGroupServerIdsAsync(Guid tenantId, Guid serverGroupId, CancellationToken cancellationToken) => await context.ServerGroupMembers
        .IgnoreQueryFilters()
        .Where(member => member.TenantId == tenantId && member.ServerGroupId == serverGroupId)
        .Select(member => member.ServerId)
        .ToListAsync(cancellationToken);
}
