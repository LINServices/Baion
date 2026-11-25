using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Results;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

/// <summary>
/// Lanza un script sobre todos los miembros de un grupo y consulta los lotes resultantes.
/// </summary>
public interface IGroupDispatchService
{
    /// <summary>
    /// Crea el lote y despacha el script a los miembros compatibles; el resto queda contabilizado como omitido o fallido.
    /// </summary>
    Task<Result<DispatchGroupResult>> DispatchAsync(DispatchGroupRequest request, string? requestedBy, CancellationToken cancellationToken);

    /// <summary>
    /// Obtiene un lote con sus ejecuciones.
    /// </summary>
    Task<Result<ExecutionBatchDetail>> GetBatchAsync(Guid batchId, CancellationToken cancellationToken);
}
