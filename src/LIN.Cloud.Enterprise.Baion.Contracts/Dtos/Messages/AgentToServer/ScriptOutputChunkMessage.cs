using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ScriptOutputChunkMessage(Guid ExecutionId, OutputStream Stream, long Sequence, string Content) : AgentToServerMessage
{
    public const string TypeDiscriminator = "script-output";
}
