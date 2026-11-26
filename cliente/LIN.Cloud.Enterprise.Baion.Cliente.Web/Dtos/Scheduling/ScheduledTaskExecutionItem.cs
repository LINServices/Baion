using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scheduling;

public record ScheduledTaskExecutionItem(Guid Id, string ScriptName, string ServerName, string Status, DateTimeOffset QueuedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, Guid? BatchId);
