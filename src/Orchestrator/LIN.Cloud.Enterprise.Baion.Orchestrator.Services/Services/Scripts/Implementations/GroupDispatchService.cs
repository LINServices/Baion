using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Results;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;
using Microsoft.Extensions.Logging;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services.Implementations;

internal class GroupDispatchService(IRepository<Script> scripts, IRepository<ServerGroup> serverGroups, IServerGroupRepository groups, IRepository<ExecutionBatch> batches, IExecutionBatchQueries batchQueries, IGroupTargetResolver resolver, IScriptDispatchService dispatch, IUnitOfWork unitOfWork, ILogger<GroupDispatchService> logger) : IGroupDispatchService
{
    private const int MaxGroupMembers = 500;

    public async Task<Result<DispatchGroupResult>> DispatchAsync(DispatchGroupRequest request, string? requestedBy, CancellationToken cancellationToken)
    {
        if (request.TimeoutSeconds is <= 0)
            return Result<DispatchGroupResult>.Failure(Error.Validation(GroupErrorCodes.TimeoutInvalid, "El timeout debe ser mayor que cero."));

        var script = await scripts.GetByIdAsync(request.ScriptId);

        if (script is null || !script.IsActive)
            return Result<DispatchGroupResult>.Failure(Error.NotFound(GroupErrorCodes.ScriptNotFound, "El script no existe o está inactivo."));

        var group = await serverGroups.GetByIdAsync(request.ServerGroupId);

        if (group is null)
            return Result<DispatchGroupResult>.Failure(Error.NotFound(GroupErrorCodes.NotFound, "El grupo de servidores no existe."));

        var members = await groups.GetMemberTargetsAsync(group.Id, cancellationToken);

        if (members.Count == 0)
            return Result<DispatchGroupResult>.Failure(Error.Validation(GroupErrorCodes.BatchGroupEmpty, "El grupo no tiene servidores."));

        if (members.Count > MaxGroupMembers)
            return Result<DispatchGroupResult>.Failure(Error.Validation(GroupErrorCodes.BatchGroupTooLarge, $"El grupo supera el máximo de {MaxGroupMembers} servidores por lanzamiento."));

        var partition = resolver.Partition(script.Runtime, members);

        if (partition.Dispatchable.Count == 0)
            return Result<DispatchGroupResult>.Failure(Error.Validation(GroupErrorCodes.BatchNoCompatibleServers, "Ningún servidor del grupo puede ejecutar este script: son incompatibles o están deshabilitados."));

        var batch = new ExecutionBatch
        {
            ServerGroupId = group.Id,
            ServerGroupName = group.Name,
            ScriptId = script.Id,
            ScriptName = script.Name,
            Mode = request.Mode,
            RequestedBy = requestedBy,
            TotalMembers = members.Count,
            SkippedIncompatibleCount = partition.SkippedIncompatible.Count,
            SkippedDisabledCount = partition.SkippedDisabled.Count
        };

        await batches.AddAsync(batch);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var outcomes = new Dictionary<Guid, BatchTargetResult>();

        foreach (var member in partition.SkippedIncompatible)
            outcomes[member.ServerId] = Skipped(member, BatchTargetOutcome.SkippedIncompatible, "execution.runtime_incompatible", $"Un script {script.Runtime} no puede ejecutarse en un servidor {member.Platform}.");

        foreach (var member in partition.SkippedDisabled)
            outcomes[member.ServerId] = Skipped(member, BatchTargetOutcome.SkippedDisabled, "server.disabled", "El servidor está desactivado y no admite ejecuciones.");

        // En serie: un DbContext por scope no es seguro entre hilos.
        foreach (var member in partition.Dispatchable)
            outcomes[member.ServerId] = await DispatchMemberAsync(batch.Id, request, member, cancellationToken);

        batch.DispatchedCount = outcomes.Values.Count(o => o.Outcome is BatchTargetOutcome.Dispatched);
        batch.FailedCount = outcomes.Values.Count(o => o.Outcome is BatchTargetOutcome.Failed);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var result in outcomes.Values.Where(o => o.Outcome is not BatchTargetOutcome.Dispatched))
            logger.LogWarning("Lote {BatchId}: servidor {ServerId} no lanzado ({Resultado}): {Motivo}", batch.Id, result.ServerId, result.Outcome, result.ErrorCode);

        logger.LogInformation("Lote {BatchId} del script {ScriptId} sobre el grupo {GroupId}: {Total} miembros, {Despachados} despachados, {Incompatibles} incompatibles, {Deshabilitados} deshabilitados, {Fallidos} fallidos", batch.Id, script.Id, group.Id, batch.TotalMembers, batch.DispatchedCount, batch.SkippedIncompatibleCount, batch.SkippedDisabledCount, batch.FailedCount);

        var targets = members.Select(m => outcomes[m.ServerId]).ToList();

        return Result<DispatchGroupResult>.Success(new DispatchGroupResult(batch.Id, batch.TotalMembers, batch.DispatchedCount, batch.SkippedIncompatibleCount, batch.SkippedDisabledCount, batch.FailedCount, targets));
    }

    public async Task<Result<ExecutionBatchDetail>> GetBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var detail = await batchQueries.GetAsync(batchId, cancellationToken);

        return detail is null
            ? Result<ExecutionBatchDetail>.Failure(Error.NotFound(GroupErrorCodes.BatchNotFound, "El lote no existe."))
            : Result<ExecutionBatchDetail>.Success(detail);
    }

    private async Task<BatchTargetResult> DispatchMemberAsync(Guid batchId, DispatchGroupRequest request, GroupMemberTarget member, CancellationToken cancellationToken)
    {
        var order = new BatchDispatch(batchId, request.ScriptId, member.ServerId, request.Mode, request.TimeoutSeconds, request.WorkingDirectory, request.EnvironmentVariables);
        var dispatched = await dispatch.DispatchBatchItemAsync(order, cancellationToken);

        if (dispatched.IsFailure)
            return new BatchTargetResult(member.ServerId, member.ServerName, BatchTargetOutcome.Failed, null, dispatched.Error!.Code, dispatched.Error.Message);

        return new BatchTargetResult(member.ServerId, member.ServerName, BatchTargetOutcome.Dispatched, dispatched.Value!.ExecutionId, null, null);
    }

    private static BatchTargetResult Skipped(GroupMemberTarget member, BatchTargetOutcome outcome, string code, string message)
    {
        return new BatchTargetResult(member.ServerId, member.ServerName, outcome, null, code, message);
    }
}
