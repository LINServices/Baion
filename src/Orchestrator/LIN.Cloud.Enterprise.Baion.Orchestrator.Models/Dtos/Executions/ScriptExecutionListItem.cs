using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Fila de un listado de ejecuciones; sin la salida, que puede pesar megabytes.
public record ScriptExecutionListItem(Guid Id, Guid ServerId, string ServerName, Guid ScriptId, string ScriptName, ExecutionStatus Status, ExecutionMode Mode, int? ExitCode, DateTimeOffset QueuedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, Guid? ChainRunId, Guid? ScheduledTaskId);
