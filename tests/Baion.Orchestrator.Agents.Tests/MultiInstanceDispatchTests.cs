using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Persistence.Context;
using Baion.Orchestrator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Dos instancias del orquestador de verdad, sobre la misma base y el mismo RabbitMQ. Es el escenario
/// que la fase 8 tenía que resolver: un comando emitido donde el agente no está conectado.
/// </summary>
public class MultiInstanceDispatchTests(MultiInstanceFixture fixture) : IClassFixture<MultiInstanceFixture>
{
    private OrchestratorFactory _instanciaA => fixture.InstanciaA;

    private OrchestratorFactory _instanciaB => fixture.InstanciaB;

    [RequiresRabbitMqFact]
    public async Task ComandoEmitidoEnA_LlegaAlAgenteConectadoEnB()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenAsync(tenantId);

        // El agente abre su socket contra B; A no tiene ninguna conexión con él.
        await using var agente = await FakeAgent.ConnectAsync(_instanciaB, token);
        await EsperarPresenciaAsync(tenantId, agente.ServerId, _instanciaB.InstanceId);

        var scriptId = await CrearScriptAsync(tenantId);

        var despacho = await DespacharDesdeAsync(_instanciaA, tenantId, scriptId, agente.ServerId);
        Assert.True(despacho.IsSuccess, despacho.Error?.Message);

        var orden = await agente.NextOrderAsync();
        Assert.Equal(despacho.Value!.ExecutionId, orden.ExecutionId);

        // El desenlace vuelve por B, que es quien tiene el socket, y queda reflejado en la base compartida.
        await agente.ReportStartedAsync(orden.ExecutionId);
        await agente.ReportOutputAsync(orden.ExecutionId, OutputStream.Stdout, "hola desde B\n");
        await agente.ReportCompletedAsync(orden.ExecutionId, ExecutionStatus.Succeeded, 0);

        var ejecucion = await EsperarTerminadaAsync(_instanciaA, tenantId, orden.ExecutionId);

        Assert.Equal(ExecutionStatus.Succeeded, ejecucion.Status);
        Assert.Equal(0, ejecucion.ExitCode);
        Assert.Equal("hola desde B\n", ejecucion.StdOut);
    }

    [RequiresRabbitMqFact]
    public async Task Despacho_HaciaUnAgenteQueNoEstaEnNingunaInstancia_DevuelveConflicto()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenAsync(tenantId);

        var agente = await FakeAgent.ConnectAsync(_instanciaB, token);
        var serverId = agente.ServerId;
        await agente.DisposeAsync();

        // Se espera a que la presencia se limpie antes de despachar: si se reintentara en bucle, los
        // intentos que aún vieran al agente en B dejarían filas y no se podría comprobar que no queda ninguna.
        await EsperarDesconexionAsync(tenantId, serverId);

        var scriptId = await CrearScriptAsync(tenantId);
        var despacho = await DespacharDesdeAsync(_instanciaA, tenantId, scriptId, serverId);

        Assert.True(despacho.IsFailure, "el despacho no debería haber salido");
        Assert.Equal("agent.not_connected", despacho.Error!.Code);
        Assert.Equal(0, await ContarEjecucionesAsync(tenantId));
    }

    [RequiresRabbitMqFact]
    public async Task ElAgenteQueReaparaceEnB_DesalojaLaConexionQueQuedabaEnA()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenAsync(tenantId);

        await using var enA = await FakeAgent.ConnectAsync(_instanciaA, token);
        Assert.True(EstaRegistradoEn(_instanciaA, enA.ServerId), "A debería tener la conexión");

        // La misma máquina reconecta contra B sin que A se entere por su socket.
        await using var enB = await FakeAgent.ConnectAsync(_instanciaB, token, ServerPlatform.Linux, enA.MachineId);
        Assert.Equal(enA.ServerId, enB.ServerId);

        var desalojada = await EsperarAsync(() => Task.FromResult(!EstaRegistradoEn(_instanciaA, enA.ServerId)));

        Assert.True(desalojada, "A debería haber cerrado su conexión al enterarse por el fanout de presencia");
    }

    private static bool EstaRegistradoEn(OrchestratorFactory instancia, Guid serverId) => instancia.Services.GetRequiredService<IAgentRegistry>().TryGet(serverId, out _);

    private async Task<Baion.Orchestrator.Models.Results.Result<ScriptExecutionDispatched>> DespacharDesdeAsync(OrchestratorFactory instancia, Guid tenantId, Guid scriptId, Guid serverId)
    {
        await using var scope = instancia.CreateTenantScope(tenantId);
        var request = new DispatchScriptRequest(scriptId, serverId, ExecutionMode.Attached, null, null, null);

        return await scope.ServiceProvider.GetRequiredService<IScriptDispatchService>().DispatchAsync(request, CancellationToken.None);
    }

    /// <summary>El servidor tarda un instante en marcarse como desconectado tras cerrarse el socket.</summary>
    private async Task EsperarDesconexionAsync(Guid tenantId, Guid serverId)
    {
        var desconectado = await EsperarAsync(async () =>
        {
            await using var scope = _instanciaA.CreateTenantScope(tenantId);
            var server = await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Servers.AsNoTracking().SingleAsync(candidate => candidate.Id == serverId);

            return server.Status is ServerStatus.Offline;
        });

        Assert.True(desconectado, "el servidor no llegó a marcarse como desconectado");
    }

    private async Task EsperarPresenciaAsync(Guid tenantId, Guid serverId, string instanceId)
    {
        var visible = await EsperarAsync(async () =>
        {
            await using var scope = _instanciaA.CreateTenantScope(tenantId);
            var server = await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Servers.AsNoTracking().SingleAsync(candidate => candidate.Id == serverId);

            return server.OrchestratorInstanceId == instanceId && server.Status is ServerStatus.Online;
        });

        Assert.True(visible, "la presencia del agente no llegó a verse desde la otra instancia");
    }

    private async Task<Guid> CrearTenantAsync()
    {
        var tenant = new Tenant { Name = "MultiInstancia", Slug = $"multi-{Guid.NewGuid():N}", IdentityMode = IdentityMode.SelfManaged };

        await using var scope = _instanciaA.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        return tenant.Id;
    }

    private async Task<string> EmitirTokenAsync(Guid tenantId)
    {
        await using var scope = _instanciaA.CreateTenantScope(tenantId);
        var emitido = await scope.ServiceProvider.GetRequiredService<IEnrollmentTokenService>().CreateAsync(new CreateEnrollmentTokenRequest("Multi", ServerKind.Vps, null, null), CancellationToken.None);

        Assert.True(emitido.IsSuccess, emitido.Error?.Message);
        return emitido.Value!.Token;
    }

    private async Task<Guid> CrearScriptAsync(Guid tenantId)
    {
        await using var scope = _instanciaA.CreateTenantScope(tenantId);
        var request = new CreateScriptRequest($"script-{Guid.NewGuid():N}", null, "echo multi", ScriptRuntime.Bash, 60);
        var creado = await scope.ServiceProvider.GetRequiredService<IScriptService>().CreateAsync(request, CancellationToken.None);

        Assert.True(creado.IsSuccess, creado.Error?.Message);
        return creado.Value!.Id;
    }

    private async Task<int> ContarEjecucionesAsync(Guid tenantId)
    {
        await using var scope = _instanciaA.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScriptExecutions.CountAsync();
    }

    private async Task<ScriptExecution> EsperarTerminadaAsync(OrchestratorFactory instancia, Guid tenantId, Guid executionId)
    {
        ScriptExecution? ejecucion = null;

        for (var intento = 0; intento < PollAttempts; intento++)
        {
            await using (var scope = instancia.CreateTenantScope(tenantId))
            {
                ejecucion = await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScriptExecutions.AsNoTracking().SingleAsync(candidate => candidate.Id == executionId);
            }

            if (ejecucion.IsFinished)
            {
                return ejecucion;
            }

            await Task.Delay(PollIntervalMilliseconds);
        }

        return ejecucion!;
    }

    private static async Task<bool> EsperarAsync(Func<Task<bool>> condicion)
    {
        for (var intento = 0; intento < PollAttempts; intento++)
        {
            if (await condicion())
            {
                return true;
            }

            await Task.Delay(PollIntervalMilliseconds);
        }

        return false;
    }

    private const int PollAttempts = 100;

    private const int PollIntervalMilliseconds = 100;
}
