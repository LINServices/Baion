using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

// DispatchDeadline: plazo para entregarla a un agente desconectado; null falla en el acto.
public record ScheduledDispatch(Guid ScheduledTaskId, Guid ScriptId, Guid ServerId, ExecutionMode Mode, DateTimeOffset? DispatchDeadline);
