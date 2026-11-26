using System;
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

    /// <summary>
    /// Ficha de una tarea del tenant actual con el nombre de su carga y de su destino. Devuelve null si no existe.
    /// </summary>
    Task<ScheduledTaskSummary?> GetSummaryAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Últimas ejecuciones lanzadas por la tarea, de la más reciente a la más antigua y sin su salida.
    /// </summary>
    Task<IReadOnlyList<ScheduledTaskExecutionItem>> ListExecutionsAsync(Guid taskId, int take, CancellationToken cancellationToken = default);
}
