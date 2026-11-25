using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Results;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;
using Microsoft.Extensions.Logging;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services.Implementations;

internal class ServerGroupService(IServerGroupRepository groups, IServerGroupQueries queries, IRepository<ServerGroupMember> members, IUnitOfWork unitOfWork, ILogger<ServerGroupService> logger) : IServerGroupService
{
    private const int MaxNameLength = 200;

    private const int MaxDescriptionLength = 1000;

    public async Task<Result<IReadOnlyList<ServerGroupSummary>>> ListAsync(CancellationToken cancellationToken)
    {
        return Result<IReadOnlyList<ServerGroupSummary>>.Success(await queries.ListAsync(cancellationToken));
    }

    public async Task<Result<ServerGroupDetail>> GetAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var detail = await queries.GetAsync(groupId, cancellationToken);

        return detail is null
            ? Result<ServerGroupDetail>.Failure(NotFound())
            : Result<ServerGroupDetail>.Success(detail);
    }

    public async Task<Result<ServerGroupDetail>> CreateAsync(CreateServerGroupRequest request, CancellationToken cancellationToken)
    {
        var serverIds = Normalize(request.ServerIds);
        var invalid = await ValidateAsync(request.Name, request.Description, serverIds, null, cancellationToken);

        if (invalid is not null)
            return Result<ServerGroupDetail>.Failure(invalid);

        var group = new ServerGroup
        {
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
            Members = serverIds.Select(id => new ServerGroupMember { ServerId = id }).ToList()
        };

        await groups.AddAsync(group);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Grupo {GroupId} '{Nombre}' creado con {Miembros} miembros", group.Id, group.Name, serverIds.Count);

        return await GetAsync(group.Id, cancellationToken);
    }

    public async Task<Result<ServerGroupDetail>> UpdateAsync(Guid groupId, UpdateServerGroupRequest request, CancellationToken cancellationToken)
    {
        var group = await groups.GetWithMembersAsync(groupId, cancellationToken);

        if (group is null)
            return Result<ServerGroupDetail>.Failure(NotFound());

        var serverIds = Normalize(request.ServerIds);
        var invalid = await ValidateAsync(request.Name, request.Description, serverIds, groupId, cancellationToken);

        if (invalid is not null)
            return Result<ServerGroupDetail>.Failure(invalid);

        group.Name = request.Name.Trim();
        group.Description = NormalizeDescription(request.Description);

        ReplaceMembers(group, serverIds);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Grupo {GroupId} actualizado: '{Nombre}' con {Miembros} miembros", group.Id, group.Name, serverIds.Count);

        return await GetAsync(group.Id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await groups.GetWithMembersAsync(groupId, cancellationToken);

        if (group is null)
            return Result.Failure(NotFound());

        if (await groups.HasScheduledTasksAsync(groupId, cancellationToken))
            return Result.Failure(Error.Conflict(GroupErrorCodes.InUseByScheduledTasks, "El grupo lo usa alguna tarea programada, activa o pausada; cambia o pausa esas tareas primero."));

        groups.Remove(group);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Grupo {GroupId} '{Nombre}' eliminado", group.Id, group.Name);

        return Result.Success();
    }

    // Conserva las pertenencias que siguen y solo toca las que sobran o faltan.
    private void ReplaceMembers(ServerGroup group, IReadOnlyList<Guid> serverIds)
    {
        var removed = group.Members.Where(m => !serverIds.Contains(m.ServerId)).ToList();
        var existing = group.Members.Select(m => m.ServerId).ToHashSet();

        foreach (var member in removed)
            members.Remove(member);

        foreach (var serverId in serverIds.Where(id => !existing.Contains(id)))
            group.Members.Add(new ServerGroupMember { ServerId = serverId });
    }

    private async Task<Error?> ValidateAsync(string? name, string? description, IReadOnlyList<Guid> serverIds, Guid? excludeGroupId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation(GroupErrorCodes.NameRequired, "El nombre del grupo es obligatorio.");

        if (name.Trim().Length > MaxNameLength)
            return Error.Validation(GroupErrorCodes.NameTooLong, $"El nombre del grupo no puede superar {MaxNameLength} caracteres.");

        if (description is not null && description.Trim().Length > MaxDescriptionLength)
            return Error.Validation(GroupErrorCodes.DescriptionTooLong, $"La descripción no puede superar {MaxDescriptionLength} caracteres.");

        if (await groups.NameExistsAsync(name.Trim(), excludeGroupId, cancellationToken))
            return Error.Conflict(GroupErrorCodes.NameTaken, "Ya existe un grupo con ese nombre.");

        if (serverIds.Count > 0 && await groups.CountServersAsync(serverIds, cancellationToken) != serverIds.Count)
            return Error.NotFound(GroupErrorCodes.ServerNotFound, "Alguno de los servidores indicados no existe.");

        return null;
    }

    private static IReadOnlyList<Guid> Normalize(IReadOnlyList<Guid>? serverIds)
    {
        return serverIds is null ? [] : serverIds.Distinct().ToList();
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private static Error NotFound()
    {
        return Error.NotFound(GroupErrorCodes.NotFound, "El grupo de servidores no existe.");
    }
}
