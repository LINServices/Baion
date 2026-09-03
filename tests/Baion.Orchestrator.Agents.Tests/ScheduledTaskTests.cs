using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Persistence;
using Baion.Orchestrator.Persistence.Context;
using Baion.Orchestrator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ScheduledTaskTests(OrchestratorFactory factory) : IClassFixture<OrchestratorFactory>
{
    [Fact]
    public async Task TareaProgramada_SobreUnGrupo_SeEjecutaEnTodosSusServidoresAlVencerElHorario()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenAsync(tenantId);

        await using var uno = await FakeAgent.ConnectAsync(factory, token);
        await using var dos = await FakeAgent.ConnectAsync(factory, token);
        await using var tres = await FakeAgent.ConnectAsync(factory, token);

        var grupoId = await CrearGrupoAsync(tenantId, [uno.ServerId, dos.ServerId, tres.ServerId]);
        var scriptId = await CrearScriptAsync(tenantId);

        var tarea = await CrearTareaAsync(tenantId, scriptId, serverGroupId: grupoId, cron: "0 3 * * *");
        Assert.True(tarea.IsSuccess, tarea.Error?.Message);

        // Se adelanta el horario en la base y el planificador la recoge en su siguiente vuelta,
        // pasando por la reserva real en lugar de llamar al disparo a mano.
        await AdelantarHorarioAsync(tenantId, tarea.Value!.Id);

        var ordenes = await Task.WhenAll(uno.NextOrderAsync(), dos.NextOrderAsync(), tres.NextOrderAsync());

        Assert.Equal(3, ordenes.Select(orden => orden.ExecutionId).Distinct().Count());

        foreach (var orden in ordenes)
        {
            await uno.ReportCompletedAsync(orden.ExecutionId, ExecutionStatus.Succeeded, 0);
        }

        var ejecuciones = await EsperarEjecucionesAsync(tenantId, 3);

        Assert.Equal(3, ejecuciones.Count);
        Assert.All(ejecuciones, ejecucion => Assert.Equal(tarea.Value.Id, ejecucion.ScheduledTaskId));
        Assert.Equal([uno.ServerId, dos.ServerId, tres.ServerId], ejecuciones.Select(ejecucion => ejecucion.ServerId).OrderBy(id => id), ServerIdComparer);

        // El calendario avanzó al siguiente disparo en lugar de repetirse.
        var refrescada = await ObtenerTareaAsync(tenantId, tarea.Value.Id);
        Assert.NotNull(refrescada.LastRunAt);
        Assert.True(refrescada.NextRunAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task TareaProgramada_ConAgenteCaido_EsperaSuMargenYLaEntregaCuandoVuelve()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenAsync(tenantId);

        var agente = await FakeAgent.ConnectAsync(factory, token);
        var serverId = agente.ServerId;
        var machineId = agente.MachineId;

        var scriptId = await CrearScriptAsync(tenantId);
        var tarea = await CrearTareaAsync(tenantId, scriptId, serverId: serverId, graceSeconds: 120);

        await agente.DisposeAsync();
        await EsperarDesconexionAsync(tenantId, serverId);

        var disparo = await DispararAsync(tenantId, tarea.Value!.Id);

        Assert.True(disparo.IsSuccess, disparo.Error?.Message);
        Assert.Equal(1, disparo.Value!.DispatchedCount);

        var enEspera = await EsperarEjecucionesAsync(tenantId, 1);
        Assert.Equal(ExecutionStatus.Pending, Assert.Single(enEspera).Status);

        // Vuelve la misma máquina: el planificador debe entregarle lo que quedó esperando.
        await using var reconectado = await FakeAgent.ConnectAsync(factory, token, ServerPlatform.Linux, machineId);
        Assert.Equal(serverId, reconectado.ServerId);

        var orden = await reconectado.NextOrderAsync();
        await reconectado.ReportCompletedAsync(orden.ExecutionId, ExecutionStatus.Succeeded, 0);

        var terminada = await EsperarEstadoAsync(tenantId, orden.ExecutionId, ExecutionStatus.Succeeded);
        Assert.Equal(ExecutionStatus.Succeeded, terminada.Status);
    }

    [Fact]
    public async Task TareaProgramada_SinMargenYAgenteCaido_NoDejaNadaEnEspera()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenAsync(tenantId);

        var agente = await FakeAgent.ConnectAsync(factory, token);
        var serverId = agente.ServerId;

        var scriptId = await CrearScriptAsync(tenantId);
        var tarea = await CrearTareaAsync(tenantId, scriptId, serverId: serverId, graceSeconds: 0);

        await agente.DisposeAsync();
        await EsperarDesconexionAsync(tenantId, serverId);

        var disparo = await DispararAsync(tenantId, tarea.Value!.Id);

        Assert.True(disparo.IsSuccess, disparo.Error?.Message);
        Assert.Equal(1, disparo.Value!.FailedCount);
        Assert.Equal(0, await ContarEjecucionesAsync(tenantId));
    }

    [Fact]
    public async Task TareaProgramada_ConUnaCadena_ArrancaElRecorridoEnElServidor()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        var primero = await CrearScriptAsync(tenantId);
        var segundo = await CrearScriptAsync(tenantId);
        var cadenaId = await CrearCadenaAsync(tenantId, [primero, segundo]);

        var tarea = await CrearTareaAsync(tenantId, scriptId: null, serverId: agente.ServerId, scriptChainId: cadenaId);
        Assert.True(tarea.IsSuccess, tarea.Error?.Message);

        var disparo = await DispararAsync(tenantId, tarea.Value!.Id);
        Assert.True(disparo.IsSuccess, disparo.Error?.Message);

        var paso1 = await agente.NextOrderAsync();
        await agente.ReportCompletedAsync(paso1.ExecutionId, ExecutionStatus.Succeeded, 0);

        var paso2 = await agente.NextOrderAsync();
        await agente.ReportCompletedAsync(paso2.ExecutionId, ExecutionStatus.Succeeded, 0);

        var ejecuciones = await EsperarEjecucionesAsync(tenantId, 2);
        Assert.All(ejecuciones, ejecucion => Assert.NotNull(ejecucion.ChainRunId));
        Assert.Single(ejecuciones.Select(ejecucion => ejecucion.ChainRunId).Distinct());
    }

    [Fact]
    public async Task Reserva_CuandoDosInstanciasVenLaMismaTarea_SoloUnaSeLlevaElDisparo()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        var scriptId = await CrearScriptAsync(tenantId);
        var tarea = await CrearTareaAsync(tenantId, scriptId, serverId: agente.ServerId, cron: "0 4 * * *");
        var original = (await ObtenerTareaAsync(tenantId, tarea.Value!.Id)).NextRunAt!.Value;

        var ahora = DateTimeOffset.UtcNow;
        var siguiente = ahora.AddHours(1);

        await using var scope = factory.CreateTenantScope(tenantId);
        var repositorio = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();

        var primera = await repositorio.TryClaimAsync(tarea.Value.Id, original, siguiente, ahora, CancellationToken.None);
        var segunda = await repositorio.TryClaimAsync(tarea.Value.Id, original, siguiente, ahora, CancellationToken.None);

        Assert.True(primera, "la primera instancia debería llevarse el disparo");
        Assert.False(segunda, "la segunda no puede llevárselo: next_run_at ya cambió");
    }

    [Theory]
    [InlineData("esto no es cron", "UTC")]
    [InlineData("0 3 * * *", "Zona/Inexistente")]
    public async Task Alta_ConCronOZonaInvalidos_SeRechaza(string cron, string zona)
    {
        var tenantId = await CrearTenantAsync();
        var scriptId = await CrearScriptAsync(tenantId);

        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new CreateScheduledTaskRequest("inválida", cron, zona, scriptId, null, Guid.CreateVersion7(), null, ExecutionMode.Attached, 60);
        var creada = await scope.ServiceProvider.GetRequiredService<IScheduledTaskService>().CreateAsync(request, CancellationToken.None);

        Assert.True(creada.IsFailure);
        Assert.Equal("task.cron_invalid", creada.Error!.Code);
    }

    [Fact]
    public async Task Alta_ConServidorYGrupoALaVez_SeRechaza()
    {
        var tenantId = await CrearTenantAsync();
        var scriptId = await CrearScriptAsync(tenantId);

        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new CreateScheduledTaskRequest("ambigua", "0 3 * * *", "UTC", scriptId, null, Guid.CreateVersion7(), Guid.CreateVersion7(), ExecutionMode.Attached, 60);
        var creada = await scope.ServiceProvider.GetRequiredService<IScheduledTaskService>().CreateAsync(request, CancellationToken.None);

        Assert.True(creada.IsFailure);
        Assert.Equal("task.target_invalid", creada.Error!.Code);
    }

    [Fact]
    public async Task Alta_RespetaLaZonaHorariaAlCalcularElPrimerDisparo()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));
        var scriptId = await CrearScriptAsync(tenantId);

        // Las 03:00 en Bogotá (UTC-5) son las 08:00 UTC.
        var tarea = await CrearTareaAsync(tenantId, scriptId, serverId: agente.ServerId, cron: "0 3 * * *", timeZoneId: "America/Bogota");

        Assert.True(tarea.IsSuccess, tarea.Error?.Message);
        Assert.Equal(8, tarea.Value!.NextRunAt!.Value.UtcDateTime.Hour);
    }

    [Fact]
    public async Task Alta_ConUnServidorQueNoExiste_DevuelveNoEncontradoEnLugarDeReventar()
    {
        var tenantId = await CrearTenantAsync();
        var scriptId = await CrearScriptAsync(tenantId);

        var tarea = await CrearTareaAsync(tenantId, scriptId, serverId: Guid.CreateVersion7());

        Assert.True(tarea.IsFailure);
        Assert.Equal("task.server_not_found", tarea.Error!.Code);
    }

    private async Task<Result<ScheduledTaskSummary>> CrearTareaAsync(Guid tenantId, Guid? scriptId, Guid? serverId = null, Guid? serverGroupId = null, Guid? scriptChainId = null, string cron = "0 3 * * *", string timeZoneId = "UTC", int graceSeconds = 60)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new CreateScheduledTaskRequest($"tarea-{Guid.NewGuid():N}", cron, timeZoneId, scriptId, scriptChainId, serverId, serverGroupId, ExecutionMode.Attached, graceSeconds);

        return await scope.ServiceProvider.GetRequiredService<IScheduledTaskService>().CreateAsync(request, CancellationToken.None);
    }

    private async Task<Result<ScheduledTaskTriggered>> DispararAsync(Guid tenantId, Guid taskId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<IScheduledTaskService>().TriggerAsync(taskId, CancellationToken.None);
    }

    private async Task AdelantarHorarioAsync(Guid tenantId, Guid taskId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        await context.ScheduledTasks
            .Where(task => task.Id == taskId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(task => task.NextRunAt, DateTimeOffset.UtcNow.AddSeconds(-1)));
    }

    private async Task<ScheduledTask> ObtenerTareaAsync(Guid tenantId, Guid taskId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScheduledTasks.AsNoTracking().SingleAsync(task => task.Id == taskId);
    }

    private async Task<Guid> CrearTenantAsync()
    {
        var tenant = new Tenant { Name = "Programadas", Slug = $"programadas-{Guid.NewGuid():N}", IdentityMode = IdentityMode.SelfManaged };

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        return tenant.Id;
    }

    private async Task<string> EmitirTokenAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var emitido = await scope.ServiceProvider.GetRequiredService<IEnrollmentTokenService>().CreateAsync(new CreateEnrollmentTokenRequest("Programadas", ServerKind.Vps, null, null), CancellationToken.None);

        Assert.True(emitido.IsSuccess, emitido.Error?.Message);
        return emitido.Value!.Token;
    }

    private async Task<Guid> CrearScriptAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new CreateScriptRequest($"script-{Guid.NewGuid():N}", null, "echo programado", ScriptRuntime.Bash, 60);
        var creado = await scope.ServiceProvider.GetRequiredService<IScriptService>().CreateAsync(request, CancellationToken.None);

        Assert.True(creado.IsSuccess, creado.Error?.Message);
        return creado.Value!.Id;
    }

    private async Task<Guid> CrearCadenaAsync(Guid tenantId, IReadOnlyList<Guid> scriptIds)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var pasos = scriptIds.Select((scriptId, indice) => new CreateScriptChainStepRequest(scriptId, indice + 1, ChainFailurePolicy.StopChain, null)).ToList();
        var creada = await scope.ServiceProvider.GetRequiredService<IScriptChainService>().CreateAsync(new CreateScriptChainRequest($"cadena-{Guid.NewGuid():N}", null, pasos), CancellationToken.None);

        Assert.True(creada.IsSuccess, creada.Error?.Message);
        return creada.Value!.Id;
    }

    private async Task<Guid> CrearGrupoAsync(Guid tenantId, IReadOnlyList<Guid> serverIds)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        var grupo = new ServerGroup { Name = $"grupo-{Guid.NewGuid():N}" };
        context.ServerGroups.Add(grupo);

        foreach (var serverId in serverIds)
        {
            context.ServerGroupMembers.Add(new ServerGroupMember { ServerGroup = grupo, ServerId = serverId });
        }

        await context.SaveChangesAsync();

        return grupo.Id;
    }

    private async Task EsperarDesconexionAsync(Guid tenantId, Guid serverId)
    {
        for (var intento = 0; intento < PollAttempts; intento++)
        {
            await using (var scope = factory.CreateTenantScope(tenantId))
            {
                var server = await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Servers.AsNoTracking().SingleAsync(candidate => candidate.Id == serverId);

                if (server.Status is ServerStatus.Offline)
                {
                    return;
                }
            }

            await Task.Delay(PollIntervalMilliseconds);
        }
    }

    private async Task<List<ScriptExecution>> EsperarEjecucionesAsync(Guid tenantId, int esperadas)
    {
        var ejecuciones = new List<ScriptExecution>();

        for (var intento = 0; intento < PollAttempts; intento++)
        {
            await using (var scope = factory.CreateTenantScope(tenantId))
            {
                ejecuciones = await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScriptExecutions.AsNoTracking().ToListAsync();
            }

            if (ejecuciones.Count >= esperadas)
            {
                return ejecuciones;
            }

            await Task.Delay(PollIntervalMilliseconds);
        }

        return ejecuciones;
    }

    private async Task<ScriptExecution> EsperarEstadoAsync(Guid tenantId, Guid executionId, ExecutionStatus esperado)
    {
        ScriptExecution? ejecucion = null;

        for (var intento = 0; intento < PollAttempts; intento++)
        {
            await using (var scope = factory.CreateTenantScope(tenantId))
            {
                ejecucion = await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScriptExecutions.AsNoTracking().SingleAsync(candidate => candidate.Id == executionId);
            }

            if (ejecucion.Status == esperado)
            {
                return ejecucion;
            }

            await Task.Delay(PollIntervalMilliseconds);
        }

        return ejecucion!;
    }

    private async Task<int> ContarEjecucionesAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScriptExecutions.CountAsync();
    }

    private static readonly IEqualityComparer<Guid> ServerIdComparer = EqualityComparer<Guid>.Default;

    private const int PollAttempts = 150;

    private const int PollIntervalMilliseconds = 100;
}
