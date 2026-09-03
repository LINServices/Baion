using System;
using Baion.Orchestrator.Models.Enums;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Paso de una cadena: qué script correr y qué hacer si falla.</summary>
public class ScriptChainStep : TenantEntity
{
    public Guid ScriptChainId { get; set; }

    public ScriptChain ScriptChain { get; set; } = null!;

    public Guid ScriptId { get; set; }

    public Script Script { get; set; } = null!;

    /// <summary>Posición dentro de la cadena, empezando en 1.</summary>
    public int Order { get; set; }

    public ChainFailurePolicy FailurePolicy { get; set; } = ChainFailurePolicy.StopChain;

    /// <summary>Sobrescribe el timeout por defecto del script cuando tiene valor.</summary>
    public int? TimeoutSecondsOverride { get; set; }
}
