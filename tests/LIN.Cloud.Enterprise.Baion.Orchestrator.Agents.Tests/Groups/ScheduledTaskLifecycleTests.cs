using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Support;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Groups;

public class ScheduledTaskLifecycleTests(OrchestratorFactory factory) : IClassFixture<OrchestratorFactory>
{
    private const string TasksUrl = "api/scheduled-tasks";

    private readonly GroupTestEnvironment _environment = new(factory);

    [Fact]
    public async Task Editar_ConTareaHabilitada_CambiaElCronYRecalculaElProximoDisparo()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var (tarea, servidorId, scriptId) = await CrearTareaDeServidorAsync(tenantId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var antes = DateTimeOffset.UtcNow;
        var respuesta = await cliente.PutAsJsonAsync($"{TasksUrl}/{tarea.Id}", Peticion("tarea-editada", "*/5 * * * *", scriptId, servidorId), GroupTestEnvironment.Json);
        var editada = await GroupTestEnvironment.ReadAsync<ScheduledTaskSummary>(respuesta);

        Assert.Equal(("tarea-editada", "*/5 * * * *"), (editada.Name, editada.CronExpression));
        Assert.Equal(("server", "script"), (editada.TargetKind, editada.PayloadKind));
        Assert.Equal(30, editada.OfflineGraceSeconds);
        Assert.True(editada.NextRunAt > antes);
        Assert.True(editada.NextRunAt <= antes.AddMinutes(5).AddSeconds(5));

        var guardada = await ObtenerTareaAsync(tenantId, tarea.Id);
        Assert.Equal(("*/5 * * * *", 30), (guardada.CronExpression, guardada.OfflineGraceSeconds));
        Assert.Equal(editada.NextRunAt, guardada.NextRunAt);
    }

    [Fact]
    public async Task Editar_ConTareaPausada_MantieneElProximoDisparoEnNull()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var (tarea, servidorId, scriptId) = await CrearTareaDeServidorAsync(tenantId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        await cliente.PostAsync($"{TasksUrl}/{tarea.Id}/disable", null);
        var editada = await GroupTestEnvironment.ReadAsync<ScheduledTaskSummary>(await cliente.PutAsJsonAsync($"{TasksUrl}/{tarea.Id}", Peticion("pausada-editada", "*/10 * * * *", scriptId, servidorId), GroupTestEnvironment.Json));

        Assert.False(editada.IsEnabled);
        Assert.Null(editada.NextRunAt);
        Assert.Null((await ObtenerTareaAsync(tenantId, tarea.Id)).NextRunAt);
    }

    [Fact]
    public async Task Editar_ConScriptSoloLinuxSobreGrupoDeWindows_Devuelve400YNoCambiaNada()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var (tarea, _, scriptId) = await CrearTareaDeServidorAsync(tenantId);
        var windows = await _environment.CreateServerAsync(tenantId, ServerPlatform.Windows);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [windows]);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var peticion = Peticion("incompatible", "0 4 * * *", scriptId, null, grupoId);
        var respuesta = await cliente.PutAsJsonAsync($"{TasksUrl}/{tarea.Id}", peticion, GroupTestEnvironment.Json);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("task.no_compatible_servers", await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        Assert.Equal(tarea.Name, (await ObtenerTareaAsync(tenantId, tarea.Id)).Name);
    }

    [Fact]
    public async Task Editar_ConCronInvalido_Devuelve400()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var (tarea, servidorId, scriptId) = await CrearTareaDeServidorAsync(tenantId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var respuesta = await cliente.PutAsJsonAsync($"{TasksUrl}/{tarea.Id}", Peticion("x", "esto no es cron", scriptId, servidorId), GroupTestEnvironment.Json);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("task.cron_invalid", await GroupTestEnvironment.ErrorCodeAsync(respuesta));
    }

    [Fact]
    public async Task Editar_ConNombreDeOtraTarea_Devuelve409()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var (tarea, servidorId, scriptId) = await CrearTareaDeServidorAsync(tenantId);
        var otra = await _environment.CreateTaskAsync(tenantId, scriptId, serverId: servidorId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var respuesta = await cliente.PutAsJsonAsync($"{TasksUrl}/{tarea.Id}", Peticion(otra.Name, "0 3 * * *", scriptId, servidorId), GroupTestEnvironment.Json);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("task.name_taken", await GroupTestEnvironment.ErrorCodeAsync(respuesta));
    }

    [Fact]
    public async Task Editar_ConTareaInexistenteOAjena_Devuelve404YNoToca()
    {
        var propio = await _environment.CreateTenantAsync();
        var ajeno = await _environment.CreateTenantAsync();
        var (tareaAjena, _, _) = await CrearTareaDeServidorAsync(ajeno);
        var servidorPropio = await _environment.CreateServerAsync(propio);
        var scriptPropio = await _environment.CreateScriptAsync(propio);
        using var cliente = _environment.CreateAdminClient(propio);

        foreach (var taskId in new[] { tareaAjena.Id, Guid.CreateVersion7() })
        {
            var respuesta = await cliente.PutAsJsonAsync($"{TasksUrl}/{taskId}", Peticion("intruso", "0 1 * * *", scriptPropio, servidorPropio), GroupTestEnvironment.Json);

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("task.not_found", await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        }

        Assert.Equal(tareaAjena.Name, (await ObtenerTareaAsync(ajeno, tareaAjena.Id)).Name);
    }

    [Fact]
    public async Task Detalle_DevuelveIdsTiposNombresYMargenParaElFormulario()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var servidorId = await _environment.CreateServerAsync(tenantId);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [servidorId], "grupo-detalle");
        var scriptId = await _environment.CreateScriptAsync(tenantId);
        var tarea = await _environment.CreateTaskAsync(tenantId, scriptId, serverGroupId: grupoId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var detalle = await GroupTestEnvironment.ReadAsync<ScheduledTaskSummary>(await cliente.GetAsync($"{TasksUrl}/{tarea.Id}"));

        Assert.Equal((scriptId, grupoId), (detalle.ScriptId, detalle.ServerGroupId));
        Assert.Equal(("script", "group", "grupo-detalle"), (detalle.PayloadKind, detalle.TargetKind, detalle.TargetName));
        Assert.Equal(60, detalle.OfflineGraceSeconds);
        Assert.Equal("UTC", detalle.TimeZoneId);
    }

    [Fact]
    public async Task Eliminar_ConTarea_Devuelve204YConservaElHistorialSinReferencia()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var (tarea, servidorId, scriptId) = await CrearTareaDeServidorAsync(tenantId);
        await SembrarEjecucionesAsync(tenantId, tarea.Id, servidorId, scriptId, 3);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var respuesta = await cliente.DeleteAsync($"{TasksUrl}/{tarea.Id}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"{TasksUrl}/{tarea.Id}")).StatusCode);

        var historial = await _environment.GetExecutionsAsync(tenantId);
        Assert.Equal(3, historial.Count);
        Assert.All(historial, ejecucion => Assert.Null(ejecucion.ScheduledTaskId));
    }

    [Fact]
    public async Task Eliminar_ConTareaInexistenteOAjena_Devuelve404YLaAjenaSigueIntacta()
    {
        var propio = await _environment.CreateTenantAsync();
        var ajeno = await _environment.CreateTenantAsync();
        var (tareaAjena, servidorAjeno, scriptAjeno) = await CrearTareaDeServidorAsync(ajeno);
        await SembrarEjecucionesAsync(ajeno, tareaAjena.Id, servidorAjeno, scriptAjeno, 1);
        using var cliente = _environment.CreateAdminClient(propio);

        foreach (var taskId in new[] { tareaAjena.Id, Guid.CreateVersion7() })
        {
            var respuesta = await cliente.DeleteAsync($"{TasksUrl}/{taskId}");

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("task.not_found", await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        }

        Assert.Equal(tareaAjena.Id, (await ObtenerTareaAsync(ajeno, tareaAjena.Id)).Id);
        Assert.Equal(tareaAjena.Id, Assert.Single(await _environment.GetExecutionsAsync(ajeno)).ScheduledTaskId);
    }

    [Fact]
    public async Task Ejecuciones_DevuelveLasUltimasDeLaTareaDeLaMasRecienteALaMasAntigua()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var (tarea, servidorId, scriptId) = await CrearTareaDeServidorAsync(tenantId);
        var otra = await _environment.CreateTaskAsync(tenantId, scriptId, serverId: servidorId);
        await SembrarEjecucionesAsync(tenantId, tarea.Id, servidorId, scriptId, 5);
        await SembrarEjecucionesAsync(tenantId, otra.Id, servidorId, scriptId, 2);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var todas = await ListarEjecucionesAsync(cliente, tarea.Id, null);
        var dos = await ListarEjecucionesAsync(cliente, tarea.Id, 2);

        Assert.Equal(5, todas.Count);
        Assert.Equal(todas.OrderByDescending(e => e.QueuedAt).Select(e => e.Id), todas.Select(e => e.Id));
        Assert.Equal(todas.Take(2).Select(e => e.Id), dos.Select(e => e.Id));
        Assert.All(todas, ejecucion => Assert.False(string.IsNullOrEmpty(ejecucion.ServerName)));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-4, 1)]
    [InlineData(1000, 100)]
    public async Task Ejecuciones_AcotaTakeEntre1Y100(int take, int esperado)
    {
        var tenantId = await _environment.CreateTenantAsync();
        var (tarea, servidorId, scriptId) = await CrearTareaDeServidorAsync(tenantId);
        await SembrarEjecucionesAsync(tenantId, tarea.Id, servidorId, scriptId, 101);
        using var cliente = _environment.CreateAdminClient(tenantId);

        Assert.Equal(esperado, (await ListarEjecucionesAsync(cliente, tarea.Id, take)).Count);
    }

    [Fact]
    public async Task Ejecuciones_ConTareaAjenaOInexistente_Devuelve404()
    {
        var propio = await _environment.CreateTenantAsync();
        var ajeno = await _environment.CreateTenantAsync();
        var (tareaAjena, servidorAjeno, scriptAjeno) = await CrearTareaDeServidorAsync(ajeno);
        await SembrarEjecucionesAsync(ajeno, tareaAjena.Id, servidorAjeno, scriptAjeno, 2);
        using var cliente = _environment.CreateAdminClient(propio);

        foreach (var taskId in new[] { tareaAjena.Id, Guid.CreateVersion7() })
        {
            var respuesta = await cliente.GetAsync($"{TasksUrl}/{taskId}/executions");

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("task.not_found", await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        }
    }

    private static UpdateScheduledTaskRequest Peticion(string name, string cron, Guid scriptId, Guid? serverId, Guid? serverGroupId = null)
    {
        return new UpdateScheduledTaskRequest(name, cron, "UTC", scriptId, null, serverId, serverGroupId, ExecutionMode.Attached, 30);
    }

    private async Task<(ScheduledTaskSummary Task, Guid ServerId, Guid ScriptId)> CrearTareaDeServidorAsync(Guid tenantId)
    {
        var servidorId = await _environment.CreateServerAsync(tenantId);
        var scriptId = await _environment.CreateScriptAsync(tenantId);

        return (await _environment.CreateTaskAsync(tenantId, scriptId, serverId: servidorId), servidorId, scriptId);
    }

    private static async Task<IReadOnlyList<ScheduledTaskExecutionItem>> ListarEjecucionesAsync(HttpClient cliente, Guid taskId, int? take)
    {
        var url = take is int valor ? $"{TasksUrl}/{taskId}/executions?take={valor}" : $"{TasksUrl}/{taskId}/executions";
        return await GroupTestEnvironment.ReadAsync<List<ScheduledTaskExecutionItem>>(await cliente.GetAsync(url));
    }

    private async Task SembrarEjecucionesAsync(Guid tenantId, Guid taskId, Guid serverId, Guid scriptId, int count)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();
        var inicio = DateTimeOffset.UtcNow.AddHours(-count - 1);

        for (var i = 0; i < count; i++)
        {
            context.ScriptExecutions.Add(new ScriptExecution
            {
                ServerId = serverId,
                ScriptId = scriptId,
                ScheduledTaskId = taskId,
                Status = ExecutionStatus.Succeeded,
                QueuedAt = inicio.AddMinutes(i)
            });
        }

        await context.SaveChangesAsync();
    }

    private async Task<ScheduledTask> ObtenerTareaAsync(Guid tenantId, Guid taskId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScheduledTasks.AsNoTracking().SingleAsync(task => task.Id == taskId);
    }
}
