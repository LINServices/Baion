using System;
using System.Collections.Generic;
using Baion.Orchestrator.Models.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Datos para dar de alta una cadena con sus pasos.</summary>
public record CreateScriptChainRequest(string Name, string? Description, IReadOnlyList<CreateScriptChainStepRequest> Steps);

/// <summary>Paso de una cadena en el alta.</summary>
/// <param name="Order">Posición dentro de la cadena, empezando en 1.</param>
/// <param name="TimeoutSecondsOverride">Sobrescribe el timeout por defecto del script cuando tiene valor.</param>
public record CreateScriptChainStepRequest(Guid ScriptId, int Order, ChainFailurePolicy FailurePolicy, int? TimeoutSecondsOverride);
