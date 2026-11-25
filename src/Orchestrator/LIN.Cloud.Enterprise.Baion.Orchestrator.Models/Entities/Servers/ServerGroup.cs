using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

public class ServerGroup : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<ServerGroupMember> Members { get; set; } = [];
}
