using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

/// <summary>Ordena al agente ejecutar un script y reportar su resultado.</summary>
public record ExecuteScriptMessage(Guid ExecutionId, string ScriptContent, string ScriptChecksum, ScriptRuntime Runtime, ExecutionMode Mode, int TimeoutSeconds, string? WorkingDirectory, IReadOnlyDictionary<string, string>? EnvironmentVariables) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "execute-script";
}
