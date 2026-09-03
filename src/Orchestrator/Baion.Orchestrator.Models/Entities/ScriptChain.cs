using System.Collections.Generic;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Secuencia de scripts que el orquestador despacha paso a paso.</summary>
public class ScriptChain : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ScriptChainStep> Steps { get; set; } = [];
}
