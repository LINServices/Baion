using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ScriptCompletionEvent(Guid TenantId, Guid ExecutionId, ExecutionStatus Status, int? ExitCode, DateTimeOffset CompletedAt, string? ErrorMessage) : ScriptExecutionEvent(TenantId, ExecutionId);
