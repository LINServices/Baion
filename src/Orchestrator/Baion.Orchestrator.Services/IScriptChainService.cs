using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Services;

/// <summary>
/// Cadenas de scripts. El recorrido lo conduce el orquestador paso a paso: el agente solo ve ejecuciones
/// sueltas, así que varias cadenas en paralelo compiten por su semáforo igual que cualquier otra ejecución.
/// </summary>
public interface IScriptChainService
{
    /// <summary>Da de alta una cadena con sus pasos.</summary>
    Task<Result<ScriptChainSummary>> CreateAsync(CreateScriptChainRequest request, CancellationToken cancellationToken);

    /// <summary>Obtiene la ficha de una cadena.</summary>
    Task<Result<ScriptChainSummary>> GetAsync(Guid chainId, CancellationToken cancellationToken);

    /// <summary>Arranca la cadena sobre un servidor y despacha su primer paso.</summary>
    Task<Result<ScriptChainRunStarted>> StartAsync(StartChainRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Evalúa el paso que acaba de terminar y despacha el siguiente si la política lo permite.
    /// Se llama una vez por cada desenlace ya persistido.
    /// </summary>
    Task AdvanceAsync(Guid executionId, CancellationToken cancellationToken);

    /// <summary>Estado agregado de un recorrido.</summary>
    Task<Result<ScriptChainRunDetail>> GetRunAsync(Guid chainRunId, CancellationToken cancellationToken);
}
