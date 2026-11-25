using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Executions;

public record DispatchScriptRequest(Guid ScriptId, Guid ServerId, string Mode, int? TimeoutSeconds, string? WorkingDirectory, IReadOnlyDictionary<string, string>? EnvironmentVariables);
