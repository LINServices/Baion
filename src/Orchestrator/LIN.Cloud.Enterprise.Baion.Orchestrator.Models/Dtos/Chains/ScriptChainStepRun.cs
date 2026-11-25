using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ScriptChainStepRun(int Order, Guid ScriptId, ChainFailurePolicy FailurePolicy, Guid? ExecutionId, ExecutionStatus? Status, int? ExitCode);
