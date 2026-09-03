using System.Collections.Generic;
using Baion.Contracts.Enums;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Script versionado que puede ejecutarse sobre uno o varios servidores.</summary>
public class Script : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Content { get; set; } = string.Empty;

    /// <summary>SHA-256 del contenido, en hexadecimal. El agente lo verifica antes de ejecutar.</summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>Se incrementa cada vez que cambia <see cref="Content"/>.</summary>
    public int Version { get; set; } = 1;

    public ScriptRuntime Runtime { get; set; }

    public int DefaultTimeoutSeconds { get; set; } = 300;

    public bool IsActive { get; set; } = true;

    public ICollection<ScriptExecution> Executions { get; set; } = [];
}
