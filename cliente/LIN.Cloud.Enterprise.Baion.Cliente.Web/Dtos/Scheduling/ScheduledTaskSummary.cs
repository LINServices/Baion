using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scheduling;

public record ScheduledTaskSummary(Guid Id, string Name, string CronExpression, string TimeZoneId, Guid? ScriptId, Guid? ScriptChainId, Guid? ServerId, Guid? ServerGroupId, string Mode, bool IsEnabled, DateTimeOffset? LastRunAt, DateTimeOffset? NextRunAt, string PayloadKind, string PayloadName, string TargetKind, string TargetName, int OfflineGraceSeconds);
