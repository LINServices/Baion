using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Baion.Orchestrator.Persistence.Implementations;

internal class ScriptQueries(BaionDbContext context) : IScriptQueries
{
    public async Task<PagedResult<ScriptListItem>> ListScriptsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var (pagina, tamano) = Pagination.Normalize(page, pageSize);
        var consulta = context.Scripts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            consulta = consulta.Where(script => script.Name.Contains(search));
        }

        var total = await consulta.CountAsync(cancellationToken);

        var elementos = await consulta
            .OrderBy(script => script.Name)
            .ThenBy(script => script.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(script => new ScriptListItem(
                script.Id,
                script.Name,
                script.Description,
                script.Runtime,
                script.Version,
                script.Checksum,
                script.DefaultTimeoutSeconds,
                script.IsActive,
                script.CreatedAt,
                script.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ScriptListItem>(elementos, pagina, tamano, total);
    }

    public async Task<ScriptDetail?> GetScriptDetailAsync(Guid scriptId, CancellationToken cancellationToken) => await context.Scripts
        .AsNoTracking()
        .Where(script => script.Id == scriptId)
        .Select(script => new ScriptDetail(
            script.Id,
            script.Name,
            script.Description,
            script.Content,
            script.Runtime,
            script.Version,
            script.Checksum,
            script.DefaultTimeoutSeconds,
            script.IsActive,
            script.CreatedAt,
            script.UpdatedAt))
        .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<ScriptExecutionListItem>> ListExecutionsAsync(ExecutionFilter filter, int page, int pageSize, CancellationToken cancellationToken)
    {
        var (pagina, tamano) = Pagination.Normalize(page, pageSize);
        var consulta = Filter(context.ScriptExecutions.AsNoTracking(), filter);
        var total = await consulta.CountAsync(cancellationToken);

        // Los nombres salen del join que genera la propia proyección: un Include traería las filas enteras,
        // y con ellas la salida acumulada, que aquí no se muestra.
        var elementos = await consulta
            .OrderByDescending(execution => execution.QueuedAt)
            .ThenByDescending(execution => execution.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(execution => new ScriptExecutionListItem(
                execution.Id,
                execution.ServerId,
                execution.Server.Name,
                execution.ScriptId,
                execution.Script.Name,
                execution.Status,
                execution.Mode,
                execution.ExitCode,
                execution.QueuedAt,
                execution.StartedAt,
                execution.CompletedAt,
                execution.ChainRunId,
                execution.ScheduledTaskId))
            .ToListAsync(cancellationToken);

        return new PagedResult<ScriptExecutionListItem>(elementos, pagina, tamano, total);
    }

    private static IQueryable<ScriptExecution> Filter(IQueryable<ScriptExecution> consulta, ExecutionFilter filter)
    {
        if (filter.ServerId is Guid serverId)
        {
            consulta = consulta.Where(execution => execution.ServerId == serverId);
        }

        if (filter.ScriptId is Guid scriptId)
        {
            consulta = consulta.Where(execution => execution.ScriptId == scriptId);
        }

        if (filter.Status is { } status)
        {
            consulta = consulta.Where(execution => execution.Status == status);
        }

        if (filter.Since is DateTimeOffset since)
        {
            consulta = consulta.Where(execution => execution.QueuedAt >= since);
        }

        return consulta;
    }
}
