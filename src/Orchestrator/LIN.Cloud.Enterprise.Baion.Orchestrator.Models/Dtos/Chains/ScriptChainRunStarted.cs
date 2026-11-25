using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ScriptChainRunStarted(Guid ChainRunId, Guid FirstExecutionId);
