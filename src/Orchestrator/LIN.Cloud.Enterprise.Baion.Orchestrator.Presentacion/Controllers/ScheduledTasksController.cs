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
