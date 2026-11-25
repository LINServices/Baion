using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

// Orden de ejecutar un script sobre un servidor como parte de un lote.
public record BatchDispatch(Guid BatchId, Guid ScriptId, Guid ServerId, ExecutionMode Mode, int? TimeoutSeconds, string? WorkingDirectory, IReadOnlyDictionary<string, string>? EnvironmentVariables);
