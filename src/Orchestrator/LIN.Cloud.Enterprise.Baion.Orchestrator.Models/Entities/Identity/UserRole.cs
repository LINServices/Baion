using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

public class UserRole : TenantEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }

    public Role Role { get; set; } = null!;
}
