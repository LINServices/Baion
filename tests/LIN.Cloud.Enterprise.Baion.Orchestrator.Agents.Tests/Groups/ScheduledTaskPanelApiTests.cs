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
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Groups;

public class ScheduledTaskPanelApiTests(OrchestratorFactory factory) : IClassFixture<OrchestratorFactory>
{
    private const string TasksUrl = "api/scheduled-tasks";

    private const string TaskNotFoundCode = "task.not_found";

    private const string CronNeverFiresCode = "task.cron_never_fires";

    private readonly GroupTestEnvironment _environment = new(factory);

    [Fact]
    public async Task Listar_EnUnTenantSinTareas_DevuelveUnaListaVacia()
    {
        var tenantId = await _environment.CreateTenantAsync();
        using var cliente = _environment.CreateAdminClient(tenantId);

        var tareas = await ListarAsync(cliente);

        Assert.Empty(tareas);
    }

    [Fact]
    public async Task Listar_ConTareaDeScriptSobreGrupo_DevuelveCargaYDestinoPorNombre()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var servidorId = await _environment.CreateServerAsync(tenantId);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [servidorId], "grupo-de-tarea");
        var scriptId = await _environment.CreateScriptAsync(tenantId);
        var tarea = await _environment.CreateTaskAsync(tenantId, scriptId, serverGroupId: grupoId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var item = Assert.Single(await ListarAsync(cliente));

        Assert.Equal(tarea.Id, item.Id);
        Assert.Equal(("script", await ObtenerNombreDeScriptAsync(tenantId, scriptId)), (item.PayloadKind, item.PayloadName));
        Assert.Equal(("group", "grupo-de-tarea"), (item.TargetKind, item.TargetName));
        Assert.True(item.IsEnabled);
        Assert.NotNull(item.NextRunAt);
    }

    [Fact]
    public async Task Listar_ConTareaDeCadenaSobreServidor_DevuelveElNombreDeLaCadena()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var servidorId = await _environment.CreateServerAsync(tenantId);
        var primero = await _environment.CreateScriptAsync(tenantId);
        var segundo = await _environment.CreateScriptAsync(tenantId);
        var cadenaId = await _environment.CreateChainAsync(tenantId, [primero, segundo]);
        await _environment.CreateTaskAsync(tenantId, null, scriptChainId: cadenaId, serverId: servidorId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var item = Assert.Single(await ListarAsync(cliente));

        Assert.Equal(("chain", await ObtenerNombreDeCadenaAsync(tenantId, cadenaId)), (item.PayloadKind, item.PayloadName));
        Assert.Equal("server", item.TargetKind);
    }

    [Fact]
    public async Task Listar_ConTareasDeOtroTenant_NoLasIncluye()
    {
        var propio = await _environment.CreateTenantAsync();
        var ajeno = await _environment.CreateTenantAsync();
        var grupoAjeno = await _environment.CreateGroupAsync(ajeno, []);
        await _environment.CreateTaskAsync(ajeno, await _environment.CreateScriptAsync(ajeno), serverGroupId: grupoAjeno);
        using var cliente = _environment.CreateAdminClient(propio);

        Assert.Empty(await ListarAsync(cliente));
    }

    [Theory]
    [InlineData("disable")]
    [InlineData("enable")]
    [InlineData("runs")]
    public async Task Accion_ConTareaInexistenteOAjena_Devuelve404TaskNotFound(string accion)
    {
        var propio = await _environment.CreateTenantAsync();
        var ajeno = await _environment.CreateTenantAsync();
        var grupoAjeno = await _environment.CreateGroupAsync(ajeno, []);
        var tareaAjena = await _environment.CreateTaskAsync(ajeno, await _environment.CreateScriptAsync(ajeno), serverGroupId: grupoAjeno);
        using var cliente = _environment.CreateAdminClient(propio);

        foreach (var taskId in new[] { tareaAjena.Id, Guid.CreateVersion7() })
        {
            var respuesta = await cliente.PostAsync($"{TasksUrl}/{taskId}/{accion}", null);

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal(TaskNotFoundCode, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        }

        Assert.True((await ObtenerTareaAsync(ajeno, tareaAjena.Id)).IsEnabled);
    }

    [Fact]
    public async Task Pausar_ConTareaYaPausada_Devuelve200Idempotente()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var tarea = await CrearTareaDeGrupoAsync(tenantId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var primera = await GroupTestEnvironment.ReadAsync<ScheduledTaskSummary>(await cliente.PostAsync($"{TasksUrl}/{tarea.Id}/disable", null));
        var segunda = await GroupTestEnvironment.ReadAsync<ScheduledTaskSummary>(await cliente.PostAsync($"{TasksUrl}/{tarea.Id}/disable", null));

        Assert.False(primera.IsEnabled);
        Assert.False(segunda.IsEnabled);
        Assert.False((await ObtenerTareaAsync(tenantId, tarea.Id)).IsEnabled);
    }

    [Fact]
    public async Task Reanudar_ConTareaPausada_ActivaYRecalculaElProximoDisparoDesdeAhora()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var tarea = await CrearTareaDeGrupoAsync(tenantId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        await cliente.PostAsync($"{TasksUrl}/{tarea.Id}/disable", null);
        await ForzarNextRunAsync(tenantId, tarea.Id, DateTimeOffset.UtcNow.AddDays(-3));

        var antes = DateTimeOffset.UtcNow;
        var reanudada = await GroupTestEnvironment.ReadAsync<ScheduledTaskSummary>(await cliente.PostAsync($"{TasksUrl}/{tarea.Id}/enable", null));

        Assert.True(reanudada.IsEnabled);
        Assert.True(reanudada.NextRunAt > antes);

        var guardada = await ObtenerTareaAsync(tenantId, tarea.Id);
        Assert.True(guardada.IsEnabled);
        Assert.Equal(reanudada.NextRunAt, guardada.NextRunAt);
    }

    [Fact]
    public async Task Reanudar_ConCronQueNoVuelveACumplirse_Devuelve400YLaTareaSigueEnPausa()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var tarea = await CrearTareaDeGrupoAsync(tenantId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        await cliente.PostAsync($"{TasksUrl}/{tarea.Id}/disable", null);

        // El 30 de febrero no existe: el cron es válido pero no tiene próxima ocurrencia.
        await CambiarCronAsync(tenantId, tarea.Id, "0 0 30 2 *");

        var respuesta = await cliente.PostAsync($"{TasksUrl}/{tarea.Id}/enable", null);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(CronNeverFiresCode, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        Assert.False((await ObtenerTareaAsync(tenantId, tarea.Id)).IsEnabled);
    }

    [Fact]
    public async Task Disparar_ConScriptSoloLinuxSobreGrupoMixto_DevuelveLosContadoresConOmitidos()
    {
        var tenantId = await _environment.CreateTenantAsync();
        await using var linux1 = await _environment.ConnectAgentAsync(tenantId);
        await using var linux2 = await _environment.ConnectAgentAsync(tenantId);
        var windows = await _environment.CreateServerAsync(tenantId, ServerPlatform.Windows);
        var deshabilitado = await _environment.CreateServerAsync(tenantId, ServerPlatform.Linux, ServerStatus.Disabled);

        var grupoId = await _environment.CreateGroupAsync(tenantId, [linux1.ServerId, linux2.ServerId, windows, deshabilitado]);
        var scriptId = await _environment.CreateScriptAsync(tenantId, ScriptRuntime.Bash);
        var tarea = await _environment.CreateTaskAsync(tenantId, scriptId, serverGroupId: grupoId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var disparo = await GroupTestEnvironment.ReadAsync<ScheduledTaskTriggered>(await cliente.PostAsync($"{TasksUrl}/{tarea.Id}/runs", null));

        Assert.Equal((4, 2, 0, 2), (disparo.TargetCount, disparo.DispatchedCount, disparo.FailedCount, disparo.SkippedCount));
        Assert.Equal(disparo.TargetCount, disparo.DispatchedCount + disparo.FailedCount + disparo.SkippedCount);
    }

    [Fact]
    public async Task Peticion_SinToken_Devuelve401()
    {
        using var cliente = _environment.CreateAnonymousClient();

        var respuesta = await cliente.GetAsync(TasksUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private static async Task<IReadOnlyList<ScheduledTaskListItem>> ListarAsync(HttpClient cliente)
    {
        return await GroupTestEnvironment.ReadAsync<List<ScheduledTaskListItem>>(await cliente.GetAsync(TasksUrl));
    }

    private async Task<ScheduledTaskSummary> CrearTareaDeGrupoAsync(Guid tenantId)
    {
        var servidorId = await _environment.CreateServerAsync(tenantId);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [servidorId]);
        var scriptId = await _environment.CreateScriptAsync(tenantId);

        return await _environment.CreateTaskAsync(tenantId, scriptId, serverGroupId: grupoId);
    }

    private async Task<string> ObtenerNombreDeScriptAsync(Guid tenantId, Guid scriptId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Scripts.Where(script => script.Id == scriptId).Select(script => script.Name).SingleAsync();
    }

    private async Task<string> ObtenerNombreDeCadenaAsync(Guid tenantId, Guid chainId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScriptChains.Where(chain => chain.Id == chainId).Select(chain => chain.Name).SingleAsync();
    }

    private async Task<Models.Entities.ScheduledTask> ObtenerTareaAsync(Guid tenantId, Guid taskId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScheduledTasks.AsNoTracking().SingleAsync(task => task.Id == taskId);
    }

    private async Task ForzarNextRunAsync(Guid tenantId, Guid taskId, DateTimeOffset nextRunAt)
    {
        await using var scope = factory.CreateTenantScope(tenantId);

        await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScheduledTasks
            .Where(task => task.Id == taskId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(task => task.NextRunAt, nextRunAt));
    }

    private async Task CambiarCronAsync(Guid tenantId, Guid taskId, string cron)
    {
        await using var scope = factory.CreateTenantScope(tenantId);

        await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScheduledTasks
            .Where(task => task.Id == taskId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(task => task.CronExpression, cron));
    }
}
