using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

public abstract class TenantEntity : Entity, ITenantOwned
{
    public Guid TenantId { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
