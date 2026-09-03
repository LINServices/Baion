using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Services;

/// <summary>Despacha ejecuciones a los agentes y refleja su avance en la base.</summary>
public interface IScriptDispatchService
{
    /// <summary>Registra la ejecución y la envía al agente conectado.</summary>
    Task<Result<ScriptExecutionDispatched>> DispatchAsync(DispatchScriptRequest request, CancellationToken cancellationToken);

    /// <summary>Despacha una ejecución nacida de una tarea programada.</summary>
    Task<Result<ScriptExecutionDispatched>> DispatchScheduledAsync(ScheduledDispatch request, CancellationToken cancellationToken);

    /// <summary>
    /// Reintenta entregar una ejecución que quedó esperando a que su agente volviera. Si venció el plazo,
    /// la marca fallida.
    /// </summary>
    Task RetryPendingAsync(Guid executionId, CancellationToken cancellationToken);

    /// <summary>Despacha un paso de cadena, dejando la ejecución enlazada con su recorrido.</summary>
    Task<Result<ScriptExecutionDispatched>> DispatchChainStepAsync(ChainStepDispatch request, CancellationToken cancellationToken);

    /// <summary>Consulta el estado y la salida acumulada de una ejecución.</summary>
    Task<Result<ScriptExecutionDetail>> GetAsync(Guid executionId, CancellationToken cancellationToken);

    /// <summary>Lista las ejecuciones que cumplen el filtro, de la más reciente a la más antigua y sin su salida.</summary>
    Task<PagedResult<ScriptExecutionListItem>> ListAsync(ExecutionFilter filter, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Aplica una novedad recibida del agente.</summary>
    Task ApplyAsync(ScriptExecutionEvent notification, CancellationToken cancellationToken);
}
