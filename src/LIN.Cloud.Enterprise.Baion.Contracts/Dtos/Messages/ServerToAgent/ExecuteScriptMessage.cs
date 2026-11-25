using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ExecuteScriptMessage(Guid ExecutionId, string ScriptContent, string ScriptChecksum, ScriptRuntime Runtime, ExecutionMode Mode, int TimeoutSeconds, string? WorkingDirectory, IReadOnlyDictionary<string, string>? EnvironmentVariables) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "execute-script";
}
