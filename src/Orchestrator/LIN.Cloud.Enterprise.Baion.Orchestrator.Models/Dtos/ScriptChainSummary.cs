using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

/// <summary>
/// Ficha de una cadena con sus pasos.
/// </summary>
public record ScriptChainSummary(Guid Id, string Name, bool IsActive, IReadOnlyList<ScriptChainStepSummary> Steps);

/// <summary>
/// Paso de una cadena.
/// </summary>
public record ScriptChainStepSummary(Guid Id, int Order, Guid ScriptId, ChainFailurePolicy FailurePolicy, int? TimeoutSecondsOverride);
