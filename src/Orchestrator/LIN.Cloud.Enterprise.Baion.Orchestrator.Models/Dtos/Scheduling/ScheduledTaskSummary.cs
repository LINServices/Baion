using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// PayloadKind: "script" o "chain". TargetKind: "server" o "group". Los nombres salen del join de la consulta.
public record ScheduledTaskSummary(Guid Id, string Name, string CronExpression, string TimeZoneId, Guid? ScriptId, Guid? ScriptChainId, Guid? ServerId, Guid? ServerGroupId, ExecutionMode Mode, bool IsEnabled, DateTimeOffset? LastRunAt, DateTimeOffset? NextRunAt, string PayloadKind, string PayloadName, string TargetKind, string TargetName, int OfflineGraceSeconds);
