using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

// Usuario de un tenant autogestionado. Los tenants en modo LIN no tienen filas aquí.
public class User : TenantEntity
{
    public string Email { get; set; } = string.Empty;

    // Email en mayúsculas invariantes; es la columna sobre la que se busca y se impone unicidad.
    public string NormalizedEmail { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    // Cambia al rotar la contraseña o revocar sesiones; invalida los tokens ya emitidos.
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginAt { get; set; }

    public int AccessFailedCount { get; set; }

    public DateTimeOffset? LockoutEndsAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
