using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Persistence.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Baion.Orchestrator.Persistence.Implementations;

internal class ScriptExecutionRepository(BaionDbContext context, ITenantContext tenantContext) : IScriptExecutionRepository
{
    public async Task<ScriptExecution?> GetByIdAsync(Guid executionId, CancellationToken cancellationToken) => await context.ScriptExecutions.FirstOrDefaultAsync(execution => execution.Id == executionId, cancellationToken);

    // Sin seguimiento: es una lectura para mostrar, y arrastrar el servidor y el script al ChangeTracker
    // solo añadiría trabajo al siguiente SaveChanges del scope.
    public async Task<ScriptExecution?> GetWithNamesAsync(Guid executionId, CancellationToken cancellationToken) => await context.ScriptExecutions
        .AsNoTracking()
        .Include(execution => execution.Server)
        .Include(execution => execution.Script)
        .FirstOrDefaultAsync(execution => execution.Id == executionId, cancellationToken);

    public async Task<IReadOnlyList<ScriptExecution>> GetPendingDispatchesAsync(int limit, CancellationToken cancellationToken) => await context.ScriptExecutions
        .IgnoreQueryFilters()
        .Where(execution => execution.Status == ExecutionStatus.Pending && execution.DispatchDeadline != null)
        .OrderBy(execution => execution.QueuedAt)
        .Take(limit)
        .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ScriptExecution>> GetByChainRunAsync(Guid chainRunId, CancellationToken cancellationToken) => await context.ScriptExecutions
        .Include(execution => execution.ScriptChainStep)
        .Where(execution => execution.ChainRunId == chainRunId)
        .OrderBy(execution => execution.QueuedAt)
        .ToListAsync(cancellationToken);

    public async Task AppendOutputAsync(Guid executionId, OutputStream stream, string content, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        // El nombre de columna sale de un enum cerrado, no de la entrada del agente.
        var column = stream is OutputStream.Stderr ? "std_err" : "std_out";

        // El filtro por tenant no se aplica al SQL crudo, así que se acota a mano.
        var sql = $"UPDATE script_executions SET {column}.WRITE(@content, NULL, 0) WHERE id = @executionId AND tenant_id = @tenantId";

        await context.Database.ExecuteSqlRawAsync(
            sql,
            [
                new SqlParameter("@content", System.Data.SqlDbType.NVarChar, -1) { Value = content },
                new SqlParameter("@executionId", executionId),
                new SqlParameter("@tenantId", tenantContext.TenantId ?? Guid.Empty)
            ],
            cancellationToken);
    }
}
