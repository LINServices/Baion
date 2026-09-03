using System;

namespace Baion.Orchestrator.Persistence.Implementations;

internal class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public void SetTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("El tenant no puede ser Guid.Empty.", nameof(tenantId));
        }

        if (TenantId is Guid actual && actual != tenantId)
        {
            throw new InvalidOperationException($"El scope ya opera sobre el tenant {actual}; no puede reasignarse a {tenantId}.");
        }

        TenantId = tenantId;
    }
}
