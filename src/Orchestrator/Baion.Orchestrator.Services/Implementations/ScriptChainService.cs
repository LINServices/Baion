using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Contracts.Enums;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Persistence;
using Microsoft.Extensions.Logging;

namespace Baion.Orchestrator.Services.Implementations;

internal class ScriptChainService(IScriptChainRepository chains, IRepository<Script> scripts, IRepository<Server> servers, IScriptExecutionRepository executionRepository, IScriptDispatchService dispatch, IUnitOfWork unitOfWork, ILogger<ScriptChainService> logger) : IScriptChainService
{
    public async Task<Result<ScriptChainSummary>> CreateAsync(CreateScriptChainRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<ScriptChainSummary>.Failure(Error.Validation("chain.name_required", "El nombre de la cadena es obligatorio."));
        }

        if (request.Steps.Count == 0)
        {
            return Result<ScriptChainSummary>.Failure(Error.Validation("chain.steps_required", "La cadena necesita al menos un paso."));
        }

        if (request.Steps.Select(step => step.Order).Distinct().Count() != request.Steps.Count)
        {
            return Result<ScriptChainSummary>.Failure(Error.Validation("chain.duplicated_order", "Dos pasos no pueden compartir la misma posición."));
        }

        if (request.Steps.Any(step => step.TimeoutSecondsOverride is <= 0))
        {
            return Result<ScriptChainSummary>.Failure(Error.Validation("chain.timeout_invalid", "El timeout de un paso debe ser mayor que cero."));
        }

        foreach (var step in request.Steps)
        {
            if (await scripts.GetByIdAsync(step.ScriptId) is null)
            {
                return Result<ScriptChainSummary>.Failure(Error.NotFound("chain.script_not_found", $"El script {step.ScriptId} del paso {step.Order} no existe."));
            }
        }

        var chain = new ScriptChain { Name = request.Name.Trim(), Description = request.Description?.Trim() };

        foreach (var step in request.Steps.OrderBy(step => step.Order))
        {
            chain.Steps.Add(new ScriptChainStep { ScriptChain = chain, ScriptId = step.ScriptId, Order = step.Order, FailurePolicy = step.FailurePolicy, TimeoutSecondsOverride = step.TimeoutSecondsOverride });
        }

        await chains.AddAsync(chain);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ScriptChainSummary>.Success(ToSummary(chain));
    }

    public async Task<Result<ScriptChainSummary>> GetAsync(Guid chainId, CancellationToken cancellationToken)
    {
        var chain = await chains.GetWithStepsAsync(chainId, cancellationToken);

        return chain is null
            ? Result<ScriptChainSummary>.Failure(Error.NotFound("chain.not_found", "La cadena no existe."))
            : Result<ScriptChainSummary>.Success(ToSummary(chain));
    }

    public async Task<Result<ScriptChainRunStarted>> StartAsync(StartChainRequest request, CancellationToken cancellationToken)
    {
        var chain = await chains.GetWithStepsAsync(request.ScriptChainId, cancellationToken);

        if (chain is null || !chain.IsActive)
        {
            return Result<ScriptChainRunStarted>.Failure(Error.NotFound("chain.not_found", "La cadena no existe o está inactiva."));
        }

        if (chain.Steps.Count == 0)
        {
            return Result<ScriptChainRunStarted>.Failure(Error.Validation("chain.steps_required", "La cadena no tiene pasos."));
        }

        var server = await servers.GetByIdAsync(request.ServerId);

        if (server is null)
        {
            return Result<ScriptChainRunStarted>.Failure(Error.NotFound("server.not_found", "El servidor no existe."));
        }

        // Se valida la cadena entera de antemano: es preferible rechazarla ahora a dejarla a medias
        // porque el paso tres resulte incompatible con la plataforma del servidor.
        var incompatible = chain.Steps.FirstOrDefault(step => !ScriptRuntimeCompatibility.IsSupported(step.Script.Runtime, server.Platform));

        if (incompatible is not null)
        {
            return Result<ScriptChainRunStarted>.Failure(Error.Validation("chain.runtime_incompatible", $"El paso {incompatible.Order} usa {incompatible.Script.Runtime} y el servidor es {server.Platform}."));
        }

        var chainRunId = Guid.CreateVersion7();
        var first = chain.Steps.OrderBy(step => step.Order).First();

        var despacho = await DispatchStepAsync(chainRunId, first, server.Id, cancellationToken);

        if (despacho is not { IsSuccess: true, Value: ScriptExecutionDispatched dispatched })
        {
            return Result<ScriptChainRunStarted>.Failure(despacho.Error!);
        }

        logger.LogInformation("Cadena {ChainId} arrancada sobre el servidor {ServerId} como recorrido {ChainRunId}", chain.Id, server.Id, chainRunId);

        return Result<ScriptChainRunStarted>.Success(new ScriptChainRunStarted(chainRunId, dispatched.ExecutionId));
    }

    public async Task AdvanceAsync(Guid executionId, CancellationToken cancellationToken)
    {
        var execution = await executionRepository.GetByIdAsync(executionId, cancellationToken);

        if (execution is null || execution.ChainRunId is not Guid chainRunId || execution.ScriptChainStepId is not Guid stepId)
        {
            return;
        }

        var step = await chains.GetStepAsync(stepId, cancellationToken);

        if (step is null)
        {
            logger.LogWarning("El paso {StepId} del recorrido {ChainRunId} ya no existe; la cadena no avanza", stepId, chainRunId);
            return;
        }

        if (execution.Status is not ExecutionStatus.Succeeded && step.FailurePolicy is ChainFailurePolicy.StopChain)
        {
            logger.LogInformation("Recorrido {ChainRunId} detenido en el paso {Orden}: terminó en {Status} y su política es parar", chainRunId, step.Order, execution.Status);
            return;
        }

        var next = step.ScriptChain.Steps.Where(candidate => candidate.Order > step.Order).OrderBy(candidate => candidate.Order).FirstOrDefault();

        if (next is null)
        {
            logger.LogInformation("Recorrido {ChainRunId} completado tras el paso {Orden}", chainRunId, step.Order);
            return;
        }

        // El índice único sobre (chain_run_id, script_chain_step_id) es la última red, pero comprobarlo
        // antes evita provocar una violación de clave si el desenlace se procesara dos veces.
        var yaLanzados = await executionRepository.GetByChainRunAsync(chainRunId, cancellationToken);

        if (yaLanzados.Any(candidate => candidate.ScriptChainStepId == next.Id))
        {
            logger.LogDebug("El paso {Orden} del recorrido {ChainRunId} ya estaba lanzado", next.Order, chainRunId);
            return;
        }

        var despacho = await DispatchStepAsync(chainRunId, next, execution.ServerId, cancellationToken);

        if (despacho.IsFailure)
        {
            logger.LogError("Recorrido {ChainRunId} interrumpido en el paso {Orden}: {Motivo}", chainRunId, next.Order, despacho.Error!.Message);
        }
    }

    public async Task<Result<ScriptChainRunDetail>> GetRunAsync(Guid chainRunId, CancellationToken cancellationToken)
    {
        var ejecuciones = await executionRepository.GetByChainRunAsync(chainRunId, cancellationToken);

        if (ejecuciones.Count == 0)
        {
            return Result<ScriptChainRunDetail>.Failure(Error.NotFound("chain_run.not_found", "El recorrido no existe."));
        }

        var primeraConPaso = ejecuciones.First(execution => execution.ScriptChainStep is not null);
        var chain = await chains.GetWithStepsAsync(primeraConPaso.ScriptChainStep!.ScriptChainId, cancellationToken);

        if (chain is null)
        {
            return Result<ScriptChainRunDetail>.Failure(Error.NotFound("chain.not_found", "La cadena del recorrido ya no existe."));
        }

        var porPaso = ejecuciones.Where(execution => execution.ScriptChainStepId is not null).ToDictionary(execution => execution.ScriptChainStepId!.Value);

        var pasos = chain.Steps
            .OrderBy(step => step.Order)
            .Select(step => ToStepRun(step, porPaso.GetValueOrDefault(step.Id)))
            .ToList();

        return Result<ScriptChainRunDetail>.Success(new ScriptChainRunDetail(chainRunId, chain.Id, primeraConPaso.ServerId, ResolveStatus(chain, pasos), pasos));
    }

    private async Task<Result<ScriptExecutionDispatched>> DispatchStepAsync(Guid chainRunId, ScriptChainStep step, Guid serverId, CancellationToken cancellationToken)
    {
        var timeout = step.TimeoutSecondsOverride ?? step.Script.DefaultTimeoutSeconds;
        return await dispatch.DispatchChainStepAsync(new ChainStepDispatch(chainRunId, step.Id, step.ScriptId, serverId, timeout), cancellationToken);
    }

    /// <summary>
    /// El estado del recorrido se deduce de sus pasos: hay uno en curso, se paró por política, llegó al
    /// final arrastrando fallos, o terminó bien.
    /// </summary>
    private static ChainRunStatus ResolveStatus(ScriptChain chain, IReadOnlyList<ScriptChainStepRun> pasos)
    {
        if (pasos.Any(paso => paso.ExecutionId is not null && paso.Status is not null && !IsFinished(paso.Status.Value)))
        {
            return ChainRunStatus.Running;
        }

        var falladosQueParan = pasos.Any(paso => paso.Status is not null && paso.Status is not ExecutionStatus.Succeeded && paso.FailurePolicy is ChainFailurePolicy.StopChain);

        if (falladosQueParan)
        {
            return ChainRunStatus.Stopped;
        }

        if (pasos.Any(paso => paso.ExecutionId is null))
        {
            return ChainRunStatus.Running;
        }

        return pasos.All(paso => paso.Status is ExecutionStatus.Succeeded) ? ChainRunStatus.Succeeded : ChainRunStatus.CompletedWithFailures;
    }

    private static bool IsFinished(ExecutionStatus status) => status is ExecutionStatus.Succeeded or ExecutionStatus.Failed or ExecutionStatus.TimedOut or ExecutionStatus.Canceled;

    private static ScriptChainStepRun ToStepRun(ScriptChainStep step, ScriptExecution? execution) => new(step.Order, step.ScriptId, step.FailurePolicy, execution?.Id, execution?.Status, execution?.ExitCode);

    private static ScriptChainSummary ToSummary(ScriptChain chain) => new(
        chain.Id,
        chain.Name,
        chain.IsActive,
        [.. chain.Steps.OrderBy(step => step.Order).Select(step => new ScriptChainStepSummary(step.Id, step.Order, step.ScriptId, step.FailurePolicy, step.TimeoutSecondsOverride))]);
}
