using System;

namespace Baion.Contracts.Messages;

/// <summary>El agente arrancó el proceso de una ejecución.</summary>
public record ScriptStartedMessage(Guid ExecutionId, DateTimeOffset StartedAt, int ProcessId) : AgentToServerMessage
{
    public const string TypeDiscriminator = "script-started";
}
