using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

/// <summary>
/// Orden de ejecutar un paso de cadena, enlazada con su recorrido.
/// </summary>
/// <param name="DispatchDeadline">Plazo para entregarla a un agente desconectado; null falla en el acto.</param>
public record ChainStepDispatch(Guid ChainRunId, Guid ScriptChainStepId, Guid ScriptId, Guid ServerId, int TimeoutSeconds)
{
    public Guid? ScheduledTaskId { get; init; }

    public DateTimeOffset? DispatchDeadline { get; init; }
}

/// <summary>
/// Orden nacida de una tarea programada.
/// </summary>
/// <param name="DispatchDeadline">Plazo para entregarla a un agente desconectado; null falla en el acto.</param>
public record ScheduledDispatch(Guid ScheduledTaskId, Guid ScriptId, Guid ServerId, ExecutionMode Mode, DateTimeOffset? DispatchDeadline);
