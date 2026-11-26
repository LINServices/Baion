using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scheduling;

public record UpdateScheduledTaskRequest(string Name, string CronExpression, string TimeZoneId, Guid? ScriptId, Guid? ScriptChainId, Guid? ServerId, Guid? ServerGroupId, string Mode, int OfflineGraceSeconds);
