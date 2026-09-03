using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Persistence;
using Microsoft.Extensions.Logging;

namespace Baion.Orchestrator.Services.Implementations;

internal class ScheduledTaskService(IScheduledTaskRepository tasks, IRepository<Script> scripts, IRepository<ScriptChain> scriptChains, IRepository<Server> servers, IRepository<ServerGroup> serverGroups, IScriptDispatchService dispatch, IScriptChainService chains, IUnitOfWork unitOfWork, TimeProvider timeProvider, ILogger<ScheduledTaskService> logger) : IScheduledTaskService
{
    public async Task<Result<ScheduledTaskSummary>> CreateAsync(CreateScheduledTaskRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<ScheduledTaskSummary>.Failure(Error.Validation("task.name_required", "El nombre de la tarea es obligatorio."));
        }

        if (!CronSchedule.IsValid(request.CronExpression, request.TimeZoneId))
        {
            return Result<ScheduledTaskSummary>.Failure(Error.Validation("task.cron_invalid", "La expresión cron o la zona horaria no son válidas."));
        }

        // Las mismas exclusiones que imponen los check constraints de la tabla, comprobadas antes de llegar a ella.
        if (request.ScriptId is null == request.ScriptChainId is null)
        {
            return Result<ScheduledTaskSummary>.Failure(Error.Validation("task.payload_invalid", "Hay que indicar un script o una cadena, y solo uno de los dos."));
        }

        if (request.ServerId is null == request.ServerGroupId is null)
        {
            return Result<ScheduledTaskSummary>.Failure(Error.Validation("task.target_invalid", "Hay que indicar un servidor o un grupo, y solo uno de los dos."));
        }

        if (request.OfflineGraceSeconds < 0)
        {
            return Result<ScheduledTaskSummary>.Failure(Error.Validation("task.grace_invalid", "El margen para agentes desconectados no puede ser negativo."));
        }

        // Se comprueban aquí: sin esto, un identificador inventado reventaría como violación de clave ajena.
        var referencias = await ValidateReferencesAsync(request);

        if (referencias is not null)
        {
            return Result<ScheduledTaskSummary>.Failure(referencias);
        }

        var now = timeProvider.GetUtcNow();

        var task = new ScheduledTask
        {
            Name = request.Name.Trim(),
            CronExpression = request.CronExpression.Trim(),
            TimeZoneId = request.TimeZoneId.Trim(),
            ScriptId = request.ScriptId,
            ScriptChainId = request.ScriptChainId,
            ServerId = request.ServerId,
            ServerGroupId = request.ServerGroupId,
            Mode = request.Mode,
            OfflineGraceSeconds = request.OfflineGraceSeconds,
            NextRunAt = CronSchedule.GetNextOccurrence(request.CronExpression, request.TimeZoneId, now)
        };

        if (task.NextRunAt is null)
        {
            return Result<ScheduledTaskSummary>.Failure(Error.Validation("task.cron_never_fires", "La expresión cron no vuelve a cumplirse."));
        }

        await tasks.AddAsync(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Tarea {TaskId} programada con cron '{Cron}' ({Zona}); primer disparo {Proximo}", task.Id, task.CronExpression, task.TimeZoneId, task.NextRunAt);

        return Result<ScheduledTaskSummary>.Success(ToSummary(task));
    }

    public async Task<Result<ScheduledTaskSummary>> GetAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(taskId, cancellationToken);

        return task is null
            ? Result<ScheduledTaskSummary>.Failure(Error.NotFound("task.not_found", "La tarea programada no existe."))
            : Result<ScheduledTaskSummary>.Success(ToSummary(task));
    }

    public async Task<Result<ScheduledTaskTriggered>> TriggerAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(taskId, cancellationToken);

        if (task is null)
        {
            return Result<ScheduledTaskTriggered>.Failure(Error.NotFound("task.not_found", "La tarea programada no existe."));
        }

        return Result<ScheduledTaskTriggered>.Success(await FireAsync(task, cancellationToken));
    }

    public async Task<ScheduledTaskTriggered> FireAsync(ScheduledTask task, CancellationToken cancellationToken)
    {
        var destinos = await ResolveTargetsAsync(task, cancellationToken);

        if (destinos.Count == 0)
        {
            logger.LogWarning("La tarea {TaskId} se disparó sin ningún servidor de destino", task.Id);
            return new ScheduledTaskTriggered(task.Id, 0, 0, 0);
        }

        var deadline = task.OfflineGraceSeconds > 0 ? timeProvider.GetUtcNow().AddSeconds(task.OfflineGraceSeconds) : (DateTimeOffset?)null;
        var entregados = 0;
        var fallidos = 0;

        // Un servidor caído no puede impedir que la tarea corra en el resto del grupo.
        foreach (var serverId in destinos)
        {
            var resultado = await FireOnServerAsync(task, serverId, deadline, cancellationToken);

            if (resultado)
            {
                entregados++;
                continue;
            }

            fallidos++;
        }

        logger.LogInformation("Tarea {TaskId} disparada sobre {Destinos} servidores: {Entregados} aceptados, {Fallidos} con error", task.Id, destinos.Count, entregados, fallidos);

        return new ScheduledTaskTriggered(task.Id, destinos.Count, entregados, fallidos);
    }

    /// <summary>Devuelve el error de la primera referencia que no exista, o null si todas son válidas.</summary>
    private async Task<Error?> ValidateReferencesAsync(CreateScheduledTaskRequest request)
    {
        if (request.ScriptId is Guid scriptId && await scripts.GetByIdAsync(scriptId) is null)
        {
            return Error.NotFound("task.script_not_found", "El script indicado no existe.");
        }

        if (request.ScriptChainId is Guid chainId && await scriptChains.GetByIdAsync(chainId) is null)
        {
            return Error.NotFound("task.chain_not_found", "La cadena indicada no existe.");
        }

        if (request.ServerId is Guid serverId && await servers.GetByIdAsync(serverId) is null)
        {
            return Error.NotFound("task.server_not_found", "El servidor indicado no existe.");
        }

        if (request.ServerGroupId is Guid groupId && await serverGroups.GetByIdAsync(groupId) is null)
        {
            return Error.NotFound("task.server_group_not_found", "El grupo de servidores indicado no existe.");
        }

        return null;
    }

    private async Task<bool> FireOnServerAsync(ScheduledTask task, Guid serverId, DateTimeOffset? deadline, CancellationToken cancellationToken)
    {
        if (task.ScriptChainId is Guid chainId)
        {
            var recorrido = await chains.StartAsync(new StartChainRequest(chainId, serverId), cancellationToken);

            if (recorrido.IsFailure)
            {
                logger.LogWarning("La tarea {TaskId} no pudo arrancar la cadena en el servidor {ServerId}: {Motivo}", task.Id, serverId, recorrido.Error!.Message);
            }

            return recorrido.IsSuccess;
        }

        var despacho = await dispatch.DispatchScheduledAsync(new ScheduledDispatch(task.Id, task.ScriptId!.Value, serverId, task.Mode, deadline), cancellationToken);

        if (despacho.IsFailure)
        {
            logger.LogWarning("La tarea {TaskId} no pudo despachar en el servidor {ServerId}: {Motivo}", task.Id, serverId, despacho.Error!.Message);
        }

        return despacho.IsSuccess;
    }

    private async Task<IReadOnlyList<Guid>> ResolveTargetsAsync(ScheduledTask task, CancellationToken cancellationToken)
    {
        if (task.ServerId is Guid serverId)
        {
            return [serverId];
        }

        if (task.ServerGroupId is Guid groupId)
        {
            return await tasks.GetGroupServerIdsAsync(task.TenantId, groupId, cancellationToken);
        }

        return [];
    }

    private static ScheduledTaskSummary ToSummary(ScheduledTask task) => new(
        task.Id,
        task.Name,
        task.CronExpression,
        task.TimeZoneId,
        task.ScriptId,
        task.ScriptChainId,
        task.ServerId,
        task.ServerGroupId,
        task.Mode,
        task.IsEnabled,
        task.LastRunAt,
        task.NextRunAt);
}
