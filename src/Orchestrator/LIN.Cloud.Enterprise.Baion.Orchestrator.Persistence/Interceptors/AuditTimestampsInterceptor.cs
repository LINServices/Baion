using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Interceptors;

internal class AuditTimestampsInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
            return;

        var now = timeProvider.GetUtcNow();
        var entries = context.ChangeTracker.Entries<Entity>().ToList();

        foreach (var entry in entries.Where(entry => entry.State is EntityState.Added))
            entry.Entity.CreatedAt = now;

        foreach (var entry in entries.Where(entry => entry.State is EntityState.Modified))
            entry.Entity.UpdatedAt = now;
    }
}
