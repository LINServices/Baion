using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ScriptOutputEvent(Guid TenantId, Guid ExecutionId, OutputStream Stream, string Content) : ScriptExecutionEvent(TenantId, ExecutionId);
