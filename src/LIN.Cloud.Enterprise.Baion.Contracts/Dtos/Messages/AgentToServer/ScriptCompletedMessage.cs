using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ScriptCompletedMessage(Guid ExecutionId, ExecutionStatus Status, int? ExitCode, DateTimeOffset CompletedAt, string? ErrorMessage) : AgentToServerMessage
{
    public const string TypeDiscriminator = "script-completed";
}
