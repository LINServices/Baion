using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;

/// <summary>
/// Consultas de solo lectura de lotes de ejecución.
/// </summary>
public interface IExecutionBatchQueries
{
    /// <summary>
    /// Lote con sus ejecuciones y sin su salida, o null si no existe en el tenant.
    /// </summary>
    Task<ExecutionBatchDetail?> GetAsync(Guid batchId, CancellationToken cancellationToken = default);
}
