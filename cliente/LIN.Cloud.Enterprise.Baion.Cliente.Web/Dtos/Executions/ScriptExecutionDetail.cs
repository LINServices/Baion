using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Executions;

public record ScriptExecutionDetail(Guid Id, Guid ServerId, string ServerName, Guid ScriptId, string ScriptName, string Status, string Mode, int? ExitCode, string? StdOut, string? StdErr, string? ErrorMessage, DateTimeOffset QueuedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt);
