using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Baion.Orchestrator.Persistence.Interceptors;

/// <summary>
/// Sella el tenant en cada inserción y bloquea cualquier escritura sobre filas de otro tenant.
/// El filtro global impide leer fuera del tenant; esto cierra el lado de la escritura.
/// </summary>
internal class TenantStampInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
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
        {
            return;
        }

        var entries = context.ChangeTracker
            .Entries<ITenantOwned>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        if (tenantContext.TenantId is not Guid tenantId)
        {
            throw new InvalidOperationException("No hay tenant en el scope actual; no se puede escribir en tablas multi-tenant.");
        }

        foreach (var entry in entries)
        {
            if (entry.State is EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = tenantId;
                continue;
            }

            if (entry.Entity.TenantId != tenantId)
            {
                throw new InvalidOperationException($"Se intentó escribir una fila del tenant {entry.Entity.TenantId} desde el tenant {tenantId}.");
            }
        }
    }
}
