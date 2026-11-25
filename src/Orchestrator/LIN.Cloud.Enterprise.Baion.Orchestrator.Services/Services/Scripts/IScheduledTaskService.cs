using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Results;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

/// <summary>
/// Tareas programadas por cron.
/// </summary>
public interface IScheduledTaskService
{
    /// <summary>
    /// Da de alta una tarea y calcula su primer disparo.
    /// </summary>
    Task<Result<ScheduledTaskSummary>> CreateAsync(CreateScheduledTaskRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Obtiene la ficha de una tarea.
    /// </summary>
    Task<Result<ScheduledTaskSummary>> GetAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>
    /// Lista las tareas del tenant con el nombre de su carga y de su destino.
    /// </summary>
    Task<Result<IReadOnlyList<ScheduledTaskListItem>>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reactiva una tarea pausada y recalcula su próximo disparo desde ahora.
    /// </summary>
    Task<Result<ScheduledTaskSummary>> EnableAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>
    /// Pausa una tarea para que el planificador no la dispare. Es idempotente.
    /// </summary>
    Task<Result<ScheduledTaskSummary>> DisableAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>
    /// Dispara la tarea ahora mismo, sin tocar su calendario. Sirve para pruebas y para operación manual.
    /// </summary>
    Task<Result<ScheduledTaskTriggered>> TriggerAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>
    /// Ejecuta el disparo de una tarea ya reservada por el planificador.
    /// </summary>
    Task<ScheduledTaskTriggered> FireAsync(ScheduledTask task, CancellationToken cancellationToken);
}
