using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Implementations;

internal class ScheduledTaskQueries(BaionDbContext context) : IScheduledTaskQueries
{
    // Los nombres de carga y destino salen de los joins de la propia proyección, en una sola consulta.
    public async Task<IReadOnlyList<ScheduledTaskListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await context.ScheduledTasks
            .AsNoTracking()
            .OrderBy(task => task.Name)
            .ThenBy(task => task.Id)
            .Select(task => new ScheduledTaskListItem(
                task.Id,
                task.Name,
                task.CronExpression,
                task.TimeZoneId,
                task.ScriptId != null ? "script" : "chain",
                task.ScriptId != null ? task.Script!.Name : task.ScriptChain!.Name,
                task.ServerGroupId != null ? "group" : "server",
                task.ServerGroupId != null ? task.ServerGroup!.Name : task.Server!.Name,
                task.Mode,
                task.IsEnabled,
                task.LastRunAt,
                task.NextRunAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ScheduledTaskSummary?> GetSummaryAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await context.ScheduledTasks
            .AsNoTracking()
            .Where(task => task.Id == taskId)
            .Select(task => new ScheduledTaskSummary(
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
                task.NextRunAt,
                task.ScriptId != null ? "script" : "chain",
                task.ScriptId != null ? task.Script!.Name : task.ScriptChain!.Name,
                task.ServerGroupId != null ? "group" : "server",
                task.ServerGroupId != null ? task.ServerGroup!.Name : task.Server!.Name,
                task.OfflineGraceSeconds))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledTaskExecutionItem>> ListExecutionsAsync(Guid taskId, int take, CancellationToken cancellationToken = default)
    {
        return await context.ScriptExecutions
            .AsNoTracking()
            .Where(execution => execution.ScheduledTaskId == taskId)
            .OrderByDescending(execution => execution.QueuedAt)
            .ThenByDescending(execution => execution.Id)
            .Take(take)
            .Select(execution => new ScheduledTaskExecutionItem(
                execution.Id,
                execution.Script.Name,
                execution.Server.Name,
                execution.Status,
                execution.QueuedAt,
                execution.StartedAt,
                execution.CompletedAt,
                execution.BatchId))
            .ToListAsync(cancellationToken);
    }
}
