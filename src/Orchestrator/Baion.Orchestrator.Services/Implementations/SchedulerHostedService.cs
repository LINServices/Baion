using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Orchestrator.Services.Implementations;

/// <summary>
/// Barre las tareas vencidas y las entregas que quedaron esperando. Corre en todas las instancias: el reparto
/// no se coordina, se resuelve reservando cada disparo con una escritura condicional, de modo que solo una
/// instancia se lo lleva y ninguna necesita saber de las demás.
/// </summary>
internal class SchedulerHostedService(IServiceScopeFactory scopeFactory, IOptions<SchedulerOptions> options, TimeProvider timeProvider, ILogger<SchedulerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        if (!settings.Enabled)
        {
            logger.LogInformation("El planificador está desactivado en esta instancia");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(settings.TickSeconds, 1)), timeProvider);

        do
        {
            try
            {
                await FireDueTasksAsync(settings, stoppingToken);
                await RetryPendingDispatchesAsync(settings, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falló una vuelta del planificador");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private async Task FireDueTasksAsync(SchedulerOptions settings, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
        var vencidas = await repository.GetDueAsync(now, settings.MaxTasksPerTick, cancellationToken);

        foreach (var task in vencidas)
        {
            await ClaimAndFireAsync(repository, task, now, cancellationToken);
        }
    }

    private async Task ClaimAndFireAsync(IScheduledTaskRepository repository, ScheduledTask task, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var siguiente = CronSchedule.GetNextOccurrence(task.CronExpression, task.TimeZoneId, now);

        if (siguiente is null)
        {
            logger.LogWarning("La tarea {TaskId} tiene una expresión cron que ya no se cumple; no se vuelve a programar", task.Id);
            return;
        }

        // Avanzar next_run_at solo si nadie lo tocó es la reserva: gana una instancia y las demás siguen.
        if (!await repository.TryClaimAsync(task.Id, task.NextRunAt!.Value, siguiente.Value, now, cancellationToken))
        {
            return;
        }

        // Scope propio con el tenant de la tarea: el disparo escribe filas que el filtro global tiene que sellar.
        await using var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(task.TenantId);

        try
        {
            await scope.ServiceProvider.GetRequiredService<IScheduledTaskService>().FireAsync(task, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falló el disparo de la tarea {TaskId}", task.Id);
        }
    }

    private async Task RetryPendingDispatchesAsync(SchedulerOptions settings, CancellationToken cancellationToken)
    {
        await using var lectura = scopeFactory.CreateAsyncScope();
        var pendientes = await lectura.ServiceProvider.GetRequiredService<IScriptExecutionRepository>().GetPendingDispatchesAsync(settings.MaxPendingDispatchesPerTick, cancellationToken);

        foreach (var pendiente in pendientes)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(pendiente.TenantId);

            try
            {
                await scope.ServiceProvider.GetRequiredService<IScriptDispatchService>().RetryPendingAsync(pendiente.Id, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falló el reintento de entrega de la ejecución {ExecutionId}", pendiente.Id);
            }
        }
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
