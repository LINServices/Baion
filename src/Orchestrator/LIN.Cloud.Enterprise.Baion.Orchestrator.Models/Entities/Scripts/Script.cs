using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

public class Script : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Content { get; set; } = string.Empty;

    // SHA-256 del contenido, en hexadecimal. El agente lo verifica antes de ejecutar.
    public string Checksum { get; set; } = string.Empty;

    // Se incrementa cada vez que cambia Content.
    public int Version { get; set; } = 1;

    public ScriptRuntime Runtime { get; set; }

    public int DefaultTimeoutSeconds { get; set; } = 300;

    public bool IsActive { get; set; } = true;

    public ICollection<ScriptExecution> Executions { get; set; } = [];
}
