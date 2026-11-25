using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Implementations;

internal class ExecutionBatchQueries(BaionDbContext context) : IExecutionBatchQueries
{
    // Una sola consulta: el lote y sus ejecuciones se proyectan juntos y sin StdOut/StdErr, que aquí no se muestran.
    public async Task<ExecutionBatchDetail?> GetAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var lote = await context.ExecutionBatches
            .AsNoTracking()
            .Where(batch => batch.Id == batchId)
            .Select(batch => new
            {
                batch.Id,
                batch.ServerGroupId,
                batch.ServerGroupName,
                batch.ScriptId,
                batch.ScriptName,
                batch.Mode,
                batch.RequestedBy,
                batch.TotalMembers,
                batch.DispatchedCount,
                batch.SkippedIncompatibleCount,
                batch.SkippedDisabledCount,
                batch.FailedCount,
                batch.CreatedAt,
                Executions = batch.Executions
                    .OrderBy(execution => execution.Server.Name)
                    .ThenBy(execution => execution.Id)
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
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return lote is null
            ? null
            : new ExecutionBatchDetail(
                lote.Id,
                lote.ServerGroupId,
                lote.ServerGroupName,
                lote.ScriptId,
                lote.ScriptName,
                lote.Mode,
                lote.RequestedBy,
                lote.TotalMembers,
                lote.DispatchedCount,
                lote.SkippedIncompatibleCount,
                lote.SkippedDisabledCount,
                lote.FailedCount,
                lote.CreatedAt,
                lote.Executions);
    }
}
