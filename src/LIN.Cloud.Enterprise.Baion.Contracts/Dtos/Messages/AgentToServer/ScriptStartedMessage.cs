using System;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ScriptStartedMessage(Guid ExecutionId, DateTimeOffset StartedAt, int ProcessId) : AgentToServerMessage
{
    public const string TypeDiscriminator = "script-started";
}
