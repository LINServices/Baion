using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

// Registra un lanzamiento de un script sobre un grupo y agrupa las ejecuciones que produjo.
public class ExecutionBatch : TenantEntity
{
    // Grupo de origen. Queda en null si el grupo se elimina.
    public Guid? ServerGroupId { get; set; }

    public ServerGroup? ServerGroup { get; set; }

    // Nombre del grupo en el momento del lanzamiento.
    public string ServerGroupName { get; set; } = string.Empty;

    public Guid ScriptId { get; set; }

    public Script Script { get; set; } = null!;

    // Nombre del script en el momento del lanzamiento.
    public string ScriptName { get; set; } = string.Empty;

    public ExecutionMode Mode { get; set; }

    // Email del usuario que pidió el lanzamiento.
    public string? RequestedBy { get; set; }

    // Miembros del grupo al lanzar.
    public int TotalMembers { get; set; }

    // Ejecuciones aceptadas y enviadas.
    public int DispatchedCount { get; set; }

    // Omitidos por plataforma no soportada.
    public int SkippedIncompatibleCount { get; set; }

    // Omitidos por servidor deshabilitado.
    public int SkippedDisabledCount { get; set; }

    // Fallidos (agente desconectado, error al enviar).
    public int FailedCount { get; set; }

    public ICollection<ScriptExecution> Executions { get; set; } = [];
}
