using System;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Support;

// Tenant vacío para migrar el esquema: migrar no lee ni escribe filas de tenant.
internal class MigrationTenantContext : ITenantContext
{
    public Guid? TenantId => null;

    public void SetTenant(Guid tenantId)
    {
        throw new InvalidOperationException("The migration context does not resolve tenants");
    }
}
