using System;
using System.Collections.Generic;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Usuario de un tenant autogestionado. Los tenants en modo LIN no tienen filas aquí.</summary>
public class User : TenantEntity
{
    public string Email { get; set; } = string.Empty;

    /// <summary>Email en mayúsculas invariantes; es la columna sobre la que se busca y se impone unicidad.</summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Cambia al rotar la contraseña o revocar sesiones; invalida los tokens ya emitidos.</summary>
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>Intentos fallidos consecutivos desde el último login correcto.</summary>
    public int AccessFailedCount { get; set; }

    /// <summary>Instante hasta el que la cuenta queda bloqueada por intentos fallidos.</summary>
    public DateTimeOffset? LockoutEndsAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
