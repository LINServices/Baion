using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scheduling;

public record ScheduledTaskTriggered(Guid ScheduledTaskId, int TargetCount, int DispatchedCount, int FailedCount, int SkippedCount);
