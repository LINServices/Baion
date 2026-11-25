using System;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ScriptChainStepSummary(Guid Id, int Order, Guid ScriptId, ChainFailurePolicy FailurePolicy, int? TimeoutSecondsOverride);
