using System;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Base de las entidades aisladas por tenant.</summary>
public abstract class TenantEntity : Entity, ITenantOwned
{
    public Guid TenantId { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
