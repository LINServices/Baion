using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

/// <summary>
/// Orden de ejecutar un script sobre un servidor concreto.
/// </summary>
/// <param name="TimeoutSeconds">Sobrescribe el timeout por defecto del script cuando tiene valor.</param>
public record DispatchScriptRequest(Guid ScriptId, Guid ServerId, ExecutionMode Mode, int? TimeoutSeconds, string? WorkingDirectory, IReadOnlyDictionary<string, string>? EnvironmentVariables);
