using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/scheduled-tasks")]
[Authorize]
public class ScheduledTasksController(IScheduledTaskService tasks) : ControllerBase
{
    /// <summary>
    /// Lista las tareas programadas del tenant con el nombre de su carga y de su destino.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        return (await tasks.ListAsync(cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Pausa una tarea programada.
    /// </summary>
    [HttpPost("{taskId:guid}/disable")]
    public async Task<IActionResult> DisableAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return (await tasks.DisableAsync(taskId, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Reactiva una tarea programada pausada.
    /// </summary>
    [HttpPost("{taskId:guid}/enable")]
    public async Task<IActionResult> EnableAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return (await tasks.EnableAsync(taskId, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Programa una tarea por cron sobre un servidor o un grupo.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateScheduledTaskRequest request, CancellationToken cancellationToken)
    {
        return (await tasks.CreateAsync(request, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Obtiene la ficha de una tarea programada.
    /// </summary>
    [HttpGet("{taskId:guid}")]
    public async Task<IActionResult> GetAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return (await tasks.GetAsync(taskId, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Dispara la tarea ahora mismo, sin alterar su calendario.
    /// </summary>
    [HttpPost("{taskId:guid}/runs")]
    public async Task<IActionResult> TriggerAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return (await tasks.TriggerAsync(taskId, cancellationToken)).ToActionResult();
    }
}
