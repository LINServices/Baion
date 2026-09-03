using System.Collections.Generic;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Agrupación de servidores para ejecución o scheduling masivo.</summary>
public class ServerGroup : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<ServerGroupMember> Members { get; set; } = [];
}
