using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scheduling;

public record ScheduledTaskListItem(Guid Id, string Name, string CronExpression, string TimeZoneId, string PayloadKind, string PayloadName, string TargetKind, string TargetName, string Mode, bool IsEnabled, DateTimeOffset? LastRunAt, DateTimeOffset? NextRunAt);
