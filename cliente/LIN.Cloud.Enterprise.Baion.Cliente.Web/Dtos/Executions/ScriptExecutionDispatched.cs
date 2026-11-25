using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Executions;

public record ScriptExecutionDispatched(Guid ExecutionId, string Status);
