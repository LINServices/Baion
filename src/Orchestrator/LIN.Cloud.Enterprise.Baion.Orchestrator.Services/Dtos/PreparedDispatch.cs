using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

// Todo lo ya resuelto y validado que necesita el envío, venga de una orden suelta, de una cadena o de una tarea.
internal record PreparedDispatch(Script Script, Server Server, ExecutionMode Mode, int TimeoutSeconds, string? WorkingDirectory, IReadOnlyDictionary<string, string>? EnvironmentVariables)
{
    public Guid? ChainRunId { get; init; }

    public Guid? ScriptChainStepId { get; init; }

    public Guid? ScheduledTaskId { get; init; }

    public Guid? BatchId { get; init; }

    public DateTimeOffset? DispatchDeadline { get; init; }
}
