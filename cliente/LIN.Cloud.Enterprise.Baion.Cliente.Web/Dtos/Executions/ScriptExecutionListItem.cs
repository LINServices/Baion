using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Executions;

public record ScriptExecutionListItem(Guid Id, Guid ServerId, string ServerName, Guid ScriptId, string ScriptName, string Status, string Mode, int? ExitCode, DateTimeOffset QueuedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, Guid? ChainRunId, Guid? ScheduledTaskId);
