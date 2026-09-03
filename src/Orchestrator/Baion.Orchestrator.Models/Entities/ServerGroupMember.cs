using System;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Pertenencia de un servidor a un grupo. Se modela explícita para que la fila también lleve tenant.</summary>
public class ServerGroupMember : TenantEntity
{
    public Guid ServerGroupId { get; set; }

    public ServerGroup ServerGroup { get; set; } = null!;

    public Guid ServerId { get; set; }

    public Server Server { get; set; } = null!;
}
