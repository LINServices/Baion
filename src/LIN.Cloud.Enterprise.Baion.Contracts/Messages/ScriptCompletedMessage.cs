using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

/// <summary>Desenlace de una ejecución. En modo Detached llega en cuanto el proceso arranca y sin código de salida.</summary>
public record ScriptCompletedMessage(Guid ExecutionId, ExecutionStatus Status, int? ExitCode, DateTimeOffset CompletedAt, string? ErrorMessage) : AgentToServerMessage
{
    public const string TypeDiscriminator = "script-completed";
}
