using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scheduling;

public record CreateScheduledTaskRequest(string Name, string CronExpression, string TimeZoneId, Guid? ScriptId, Guid? ScriptChainId, Guid? ServerId, Guid? ServerGroupId, string Mode, int OfflineGraceSeconds);
