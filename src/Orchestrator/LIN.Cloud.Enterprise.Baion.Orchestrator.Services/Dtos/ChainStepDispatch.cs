using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

// DispatchDeadline: plazo para entregarla a un agente desconectado; null falla en el acto.
public record ChainStepDispatch(Guid ChainRunId, Guid ScriptChainStepId, Guid ScriptId, Guid ServerId, int TimeoutSeconds)
{
    public Guid? ScheduledTaskId { get; init; }

    public DateTimeOffset? DispatchDeadline { get; init; }
}
