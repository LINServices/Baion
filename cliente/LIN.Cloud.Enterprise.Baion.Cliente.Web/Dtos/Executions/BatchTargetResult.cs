using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Executions;

public record BatchTargetResult(Guid ServerId, string ServerName, string Outcome, Guid? ExecutionId, string? ErrorCode, string? Message);
