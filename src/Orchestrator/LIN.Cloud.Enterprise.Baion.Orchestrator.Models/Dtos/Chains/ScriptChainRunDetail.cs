using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Estado de un recorrido. No hay tabla propia: se deduce de las ejecuciones que comparten
// chain_run_id, que es lo que correlaciona los pasos de un mismo recorrido.
public record ScriptChainRunDetail(Guid ChainRunId, Guid ScriptChainId, Guid ServerId, ChainRunStatus Status, IReadOnlyList<ScriptChainStepRun> Steps);
