using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// PayloadKind: "script" o "chain". TargetKind: "server" o "group".
public record ScheduledTaskListItem(Guid Id, string Name, string CronExpression, string TimeZoneId, string PayloadKind, string PayloadName, string TargetKind, string TargetName, ExecutionMode Mode, bool IsEnabled, DateTimeOffset? LastRunAt, DateTimeOffset? NextRunAt);
