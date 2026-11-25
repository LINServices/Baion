using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;

/// <summary>
/// Consultas de solo lectura de tareas programadas para el panel.
/// </summary>
public interface IScheduledTaskQueries
{
    /// <summary>
    /// Tareas del tenant actual con el nombre de su carga y de su destino.
    /// </summary>
    Task<IReadOnlyList<ScheduledTaskListItem>> ListAsync(CancellationToken cancellationToken = default);
}
