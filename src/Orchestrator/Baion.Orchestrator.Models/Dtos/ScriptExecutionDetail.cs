using System;
using Baion.Contracts.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Estado completo de una ejecución, con su salida acumulada.</summary>
public record ScriptExecutionDetail(Guid Id, Guid ServerId, string ServerName, Guid ScriptId, string ScriptName, ExecutionStatus Status, ExecutionMode Mode, int? ExitCode, string? StdOut, string? StdErr, string? ErrorMessage, DateTimeOffset QueuedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt);
