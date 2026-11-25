using System;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

public class ScriptChainStep : TenantEntity
{
    public Guid ScriptChainId { get; set; }

    public ScriptChain ScriptChain { get; set; } = null!;

    public Guid ScriptId { get; set; }

    public Script Script { get; set; } = null!;

    // Posición dentro de la cadena, empezando en 1.
    public int Order { get; set; }

    public ChainFailurePolicy FailurePolicy { get; set; } = ChainFailurePolicy.StopChain;

    // Sobrescribe el timeout por defecto del script cuando tiene valor.
    public int? TimeoutSecondsOverride { get; set; }
}
