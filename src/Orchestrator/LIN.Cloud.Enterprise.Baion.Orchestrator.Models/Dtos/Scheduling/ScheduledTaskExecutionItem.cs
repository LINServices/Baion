using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Ejecución lanzada por una tarea; sin la salida, que puede pesar megabytes.
public record ScheduledTaskExecutionItem(Guid Id, string ScriptName, string ServerName, ExecutionStatus Status, DateTimeOffset QueuedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, Guid? BatchId);
