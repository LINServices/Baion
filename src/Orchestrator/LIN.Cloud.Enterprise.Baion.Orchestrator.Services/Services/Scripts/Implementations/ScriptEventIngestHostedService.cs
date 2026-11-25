using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services.Implementations;

// Vacía el buzón de novedades de ejecución. Procesa en el orden en que llegaron y junta los fragmentos
// de salida consecutivos del mismo flujo en una sola escritura, sin adelantar nunca el desenlace a la
// salida que lo precede.
internal class ScriptEventIngestHostedService(IScriptEventQueue queue, IServiceScopeFactory scopeFactory, IOptions<ScriptEventOptions> options, TimeProvider timeProvider, ILogger<ScriptEventIngestHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (queue is not ScriptEventQueue source)
        {
            logger.LogError("El buzón de novedades registrado no es {Tipo}; no se escribirá ninguna", nameof(ScriptEventQueue));
            return;
        }

        var settings = options.Value;
        var batch = new List<ScriptExecutionEvent>(settings.BatchSize);
        var window = TimeSpan.FromMilliseconds(Math.Max(settings.BatchWindowMilliseconds, 1));

        while (!stoppingToken.IsCancellationRequested && await SafeWaitToReadAsync(source, stoppingToken))
        {
            await FillBatchAsync(source, batch, settings.BatchSize, window, stoppingToken);
            await FlushAsync(batch, stoppingToken);
        }

        await FlushAsync(batch, CancellationToken.None);
    }

    private async Task FillBatchAsync(ScriptEventQueue source, List<ScriptExecutionEvent> batch, int batchSize, TimeSpan window, CancellationToken stoppingToken)
    {
        var deadline = timeProvider.GetUtcNow() + window;

        var hayMas = true;

        while (hayMas && batch.Count < batchSize)
            hayMas = await TryTakeNextAsync(source, batch, deadline, stoppingToken);
    }

    // Toma la siguiente entrada o espera a que llegue como máximo hasta el límite. Devuelve false
    // cuando ya no hay que seguir llenando el lote (ventana vencida, buzón cerrado o parada).
    private async Task<bool> TryTakeNextAsync(ScriptEventQueue source, List<ScriptExecutionEvent> batch, DateTimeOffset deadline, CancellationToken stoppingToken)
    {
        if (source.TryRead(out var notification))
        {
            batch.Add(notification);
            return true;
        }

        var restante = deadline - timeProvider.GetUtcNow();

        if (restante <= TimeSpan.Zero)
            return false;

        using var ventana = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        ventana.CancelAfter(restante);

        try
        {
            return await source.WaitToReadAsync(ventana.Token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    // Recorre el lote en orden. La salida se acumula por ejecución y flujo, y se vuelca justo antes de
    // aplicar cualquier otra novedad de esa misma ejecución: así el desenlace nunca se adelanta a su salida.
    private async Task FlushAsync(List<ScriptExecutionEvent> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
            return;

        var buffers = new Dictionary<OutputKey, StringBuilder>();
        var scopes = new Dictionary<Guid, AsyncServiceScope>();

        try
        {
            foreach (var notification in batch)
                await ProcessAsync(notification, buffers, scopes, cancellationToken);

            await FlushBuffersAsync(buffers, executionId: null, scopes, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo escribir un lote de {Novedades} novedades de ejecución", batch.Count);
        }
        finally
        {
            foreach (var scope in scopes.Values)
                await scope.DisposeAsync();

            batch.Clear();
        }
    }

    private async Task ProcessAsync(ScriptExecutionEvent notification, Dictionary<OutputKey, StringBuilder> buffers, Dictionary<Guid, AsyncServiceScope> scopes, CancellationToken cancellationToken)
    {
        if (notification is ScriptOutputEvent output)
        {
            Buffer(buffers, output);
            return;
        }

        await FlushBuffersAsync(buffers, notification.ExecutionId, scopes, cancellationToken);
        await ApplyAsync(scopes, notification, cancellationToken);
    }

    private async Task FlushBuffersAsync(Dictionary<OutputKey, StringBuilder> buffers, Guid? executionId, Dictionary<Guid, AsyncServiceScope> scopes, CancellationToken cancellationToken)
    {
        if (buffers.Count == 0)
            return;

        var pendientes = buffers.Keys.Where(clave => executionId is null || clave.ExecutionId == executionId).ToList();

        foreach (var clave in pendientes)
        {
            var contenido = buffers[clave].ToString();
            buffers.Remove(clave);

            await ApplyAsync(scopes, new ScriptOutputEvent(clave.TenantId, clave.ExecutionId, clave.Stream, contenido), cancellationToken);
        }
    }

    private async Task ApplyAsync(Dictionary<Guid, AsyncServiceScope> scopes, ScriptExecutionEvent notification, CancellationToken cancellationToken)
    {
        var scope = ResolveScope(scopes, notification.TenantId);
        await scope.ServiceProvider.GetRequiredService<IScriptDispatchService>().ApplyAsync(notification, cancellationToken);

        if (notification is ScriptCompletionEvent)
            await AdvanceChainAsync(scope, notification, cancellationToken);
    }

    // La cadena avanza después de persistir el desenlace, nunca antes. Se aísla del resto del lote:
    // un problema despachando el siguiente paso no puede tumbar la escritura de las demás novedades.
    private async Task AdvanceChainAsync(AsyncServiceScope scope, ScriptExecutionEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await scope.ServiceProvider.GetRequiredService<IScriptChainService>().AdvanceAsync(notification.ExecutionId, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo avanzar la cadena de la ejecución {ExecutionId}", notification.ExecutionId);
        }
    }

    // Un scope por tenant dentro del lote: cada uno solo puede operar sobre el suyo.
    private AsyncServiceScope ResolveScope(Dictionary<Guid, AsyncServiceScope> scopes, Guid tenantId)
    {
        if (scopes.TryGetValue(tenantId, out var existente))
            return existente;

        var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(tenantId);
        scopes[tenantId] = scope;

        return scope;
    }

    private static async Task<bool> SafeWaitToReadAsync(ScriptEventQueue source, CancellationToken stoppingToken)
    {
        try
        {
            return await source.WaitToReadAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static void Buffer(Dictionary<OutputKey, StringBuilder> buffers, ScriptOutputEvent output)
    {
        var clave = new OutputKey(output.TenantId, output.ExecutionId, output.Stream);

        if (!buffers.TryGetValue(clave, out var builder))
        {
            builder = new StringBuilder();
            buffers[clave] = builder;
        }

        builder.Append(output.Content);
    }
}
