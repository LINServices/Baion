using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ExecutionBatchDetail(Guid Id, Guid? ServerGroupId, string ServerGroupName, Guid ScriptId, string ScriptName, ExecutionMode Mode, string? RequestedBy, int TotalMembers, int DispatchedCount, int SkippedIncompatibleCount, int SkippedDisabledCount, int FailedCount, DateTimeOffset CreatedAt, IReadOnlyList<ScriptExecutionListItem> Executions);
