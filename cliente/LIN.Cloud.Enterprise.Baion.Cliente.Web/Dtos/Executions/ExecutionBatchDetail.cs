using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Executions;

public record ExecutionBatchDetail(Guid Id, Guid? ServerGroupId, string ServerGroupName, Guid ScriptId, string ScriptName, string Mode, string? RequestedBy, int TotalMembers, int DispatchedCount, int SkippedIncompatibleCount, int SkippedDisabledCount, int FailedCount, DateTimeOffset CreatedAt, IReadOnlyList<ScriptExecutionListItem> Executions);
