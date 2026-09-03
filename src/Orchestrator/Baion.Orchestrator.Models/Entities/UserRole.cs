using System;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Asignación de un rol a un usuario dentro del mismo tenant.</summary>
public class UserRole : TenantEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }

    public Role Role { get; set; } = null!;
}
