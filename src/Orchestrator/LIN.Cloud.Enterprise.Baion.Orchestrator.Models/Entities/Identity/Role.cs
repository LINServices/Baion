using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

public class Role : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    // Nombre en mayúsculas invariantes; es la columna sobre la que se busca y se impone unicidad.
    public string NormalizedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
