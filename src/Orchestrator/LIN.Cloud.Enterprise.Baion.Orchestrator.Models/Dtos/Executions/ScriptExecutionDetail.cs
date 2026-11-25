using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ScriptExecutionDetail(Guid Id, Guid ServerId, string ServerName, Guid ScriptId, string ScriptName, ExecutionStatus Status, ExecutionMode Mode, int? ExitCode, string? StdOut, string? StdErr, string? ErrorMessage, DateTimeOffset QueuedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt);
