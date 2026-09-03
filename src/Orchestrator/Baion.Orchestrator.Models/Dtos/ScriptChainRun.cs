using System;
using System.Collections.Generic;
using Baion.Contracts.Enums;
using Baion.Orchestrator.Models.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Arranque de una cadena sobre un servidor.</summary>
public record StartChainRequest(Guid ScriptChainId, Guid ServerId);

/// <summary>Confirmación de que la cadena arrancó y su primer paso salió hacia el agente.</summary>
public record ScriptChainRunStarted(Guid ChainRunId, Guid FirstExecutionId);

/// <summary>
/// Estado de un recorrido. No hay tabla propia: se deduce de las ejecuciones que comparten
/// <c>chain_run_id</c>, que es lo que correlaciona los pasos de un mismo recorrido.
/// </summary>
public record ScriptChainRunDetail(Guid ChainRunId, Guid ScriptChainId, Guid ServerId, ChainRunStatus Status, IReadOnlyList<ScriptChainStepRun> Steps);

/// <summary>Resultado de un paso dentro del recorrido.</summary>
public record ScriptChainStepRun(int Order, Guid ScriptId, ChainFailurePolicy FailurePolicy, Guid? ExecutionId, ExecutionStatus? Status, int? ExitCode);
