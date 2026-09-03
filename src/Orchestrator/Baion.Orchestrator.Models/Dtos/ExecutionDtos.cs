using System;
using Baion.Contracts.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Fila de un listado de ejecuciones; sin la salida, que puede pesar megabytes.</summary>
public record ScriptExecutionListItem(Guid Id, Guid ServerId, string ServerName, Guid ScriptId, string ScriptName, ExecutionStatus Status, ExecutionMode Mode, int? ExitCode, DateTimeOffset QueuedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, Guid? ChainRunId, Guid? ScheduledTaskId);

/// <summary>Criterios de búsqueda de ejecuciones; cada campo nulo no filtra.</summary>
public record ExecutionFilter(Guid? ServerId, Guid? ScriptId, ExecutionStatus? Status, DateTimeOffset? Since);
