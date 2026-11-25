using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;

internal sealed class DesignTimeTenantContext : ITenantContext
{
    public Guid? TenantId => null;

    public void SetTenant(Guid tenantId)
    {
        throw new NotSupportedException("El contexto de diseño no opera sobre un tenant.");
    }
}
