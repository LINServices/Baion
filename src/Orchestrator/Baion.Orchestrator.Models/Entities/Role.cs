using System.Collections.Generic;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Rol definido por un tenant autogestionado.</summary>
public class Role : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Nombre en mayúsculas invariantes; es la columna sobre la que se busca y se impone unicidad.</summary>
    public string NormalizedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
