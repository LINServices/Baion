using System.Threading;
using System.Threading.Tasks;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;

/// <summary>
/// Confirma en bloque los cambios acumulados por los repositorios del scope.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persiste los cambios pendientes y devuelve el número de filas afectadas.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
