using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ScheduledTaskSummary(Guid Id, string Name, string CronExpression, string TimeZoneId, Guid? ScriptId, Guid? ScriptChainId, Guid? ServerId, Guid? ServerGroupId, ExecutionMode Mode, bool IsEnabled, DateTimeOffset? LastRunAt, DateTimeOffset? NextRunAt);
