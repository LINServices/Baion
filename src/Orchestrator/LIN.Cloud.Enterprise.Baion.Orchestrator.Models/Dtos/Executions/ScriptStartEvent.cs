using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ScriptStartEvent(Guid TenantId, Guid ExecutionId, DateTimeOffset StartedAt) : ScriptExecutionEvent(TenantId, ExecutionId);
