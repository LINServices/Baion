using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Services;

/// <summary>Tareas programadas por cron.</summary>
public interface IScheduledTaskService
{
    /// <summary>Da de alta una tarea y calcula su primer disparo.</summary>
    Task<Result<ScheduledTaskSummary>> CreateAsync(CreateScheduledTaskRequest request, CancellationToken cancellationToken);

    /// <summary>Obtiene la ficha de una tarea.</summary>
    Task<Result<ScheduledTaskSummary>> GetAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>Dispara la tarea ahora mismo, sin tocar su calendario. Sirve para pruebas y para operación manual.</summary>
    Task<Result<ScheduledTaskTriggered>> TriggerAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>Ejecuta el disparo de una tarea ya reservada por el planificador.</summary>
    Task<ScheduledTaskTriggered> FireAsync(ScheduledTask task, CancellationToken cancellationToken);
}
