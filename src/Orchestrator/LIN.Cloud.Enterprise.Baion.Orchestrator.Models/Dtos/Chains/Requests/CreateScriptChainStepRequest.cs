using System;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Order: Posición dentro de la cadena, empezando en 1.
// TimeoutSecondsOverride: Sobrescribe el timeout por defecto del script cuando tiene valor.
public record CreateScriptChainStepRequest(Guid ScriptId, int Order, ChainFailurePolicy FailurePolicy, int? TimeoutSecondsOverride);
