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
using Baion.Orchestrator.Persistence.Context;
using Baion.Orchestrator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ScriptDispatchTests(OrchestratorFactory factory) : IClassFixture<OrchestratorFactory>
{
    [Fact]
    public async Task TresEjecucionesEnParalelo_UnaDetached_QuedanReflejadasEnLaBase()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        var scriptId = await CrearScriptAsync(tenantId, "echo hola", ScriptRuntime.Bash);

        // Los tres salen sin esperar unos por otros: el orquestador no serializa el despacho.
        var despachos = await Task.WhenAll(
            DespacharAsync(tenantId, scriptId, agente.ServerId, ExecutionMode.Attached),
            DespacharAsync(tenantId, scriptId, agente.ServerId, ExecutionMode.Attached),
            DespacharAsync(tenantId, scriptId, agente.ServerId, ExecutionMode.Detached));

        Assert.All(despachos, despacho => Assert.True(despacho.IsSuccess, despacho.Error?.Message));

        var ordenes = new List<Baion.Contracts.Messages.ExecuteScriptMessage>();

        for (var indice = 0; indice < 3; indice++)
        {
            ordenes.Add(await agente.NextOrderAsync());
        }

        Assert.Equal(3, ordenes.Select(orden => orden.ExecutionId).Distinct().Count());
        Assert.Single(ordenes, orden => orden.Mode is ExecutionMode.Detached);

        foreach (var orden in ordenes)
        {
            await agente.ReportStartedAsync(orden.ExecutionId);

            if (orden.Mode is ExecutionMode.Detached)
            {
                // Lanzamiento correcto sin código de salida: el agente no llega a observarlo.
                await agente.ReportCompletedAsync(orden.ExecutionId, ExecutionStatus.Succeeded, null);
                continue;
            }

            await agente.ReportOutputAsync(orden.ExecutionId, OutputStream.Stdout, "hola\n");
            await agente.ReportCompletedAsync(orden.ExecutionId, ExecutionStatus.Succeeded, 0);
        }

        var ejecuciones = await EsperarEjecucionesTerminadasAsync(tenantId, 3);

        Assert.Equal(3, ejecuciones.Count);
        Assert.All(ejecuciones, ejecucion => Assert.Equal(ExecutionStatus.Succeeded, ejecucion.Status));
        Assert.All(ejecuciones, ejecucion => Assert.Equal(agente.ServerId, ejecucion.ServerId));

        var adjuntas = ejecuciones.Where(ejecucion => ejecucion.Mode is ExecutionMode.Attached).ToList();
        Assert.Equal(2, adjuntas.Count);
        Assert.All(adjuntas, ejecucion => Assert.Equal(0, ejecucion.ExitCode));
        Assert.All(adjuntas, ejecucion => Assert.Equal("hola\n", ejecucion.StdOut));

        var desatendida = Assert.Single(ejecuciones, ejecucion => ejecucion.Mode is ExecutionMode.Detached);
        Assert.Null(desatendida.ExitCode);
        Assert.Equal(string.Empty, desatendida.StdOut);
    }

    [Fact]
    public async Task Salida_TroceadaEnMuchosFragmentos_LlegaCompletaYAntesDelDesenlace()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        var scriptId = await CrearScriptAsync(tenantId, "salida larga", ScriptRuntime.Bash);
        var despacho = await DespacharAsync(tenantId, scriptId, agente.ServerId, ExecutionMode.Attached);
        var orden = await agente.NextOrderAsync();

        await agente.ReportStartedAsync(orden.ExecutionId);

        const int fragmentos = 200;
        var esperado = string.Concat(Enumerable.Range(0, fragmentos).Select(indice => $"linea-{indice}\n"));

        for (var indice = 0; indice < fragmentos; indice++)
        {
            await agente.ReportOutputAsync(orden.ExecutionId, OutputStream.Stdout, $"linea-{indice}\n", indice);
        }

        await agente.ReportCompletedAsync(orden.ExecutionId, ExecutionStatus.Succeeded, 0);

        var ejecucion = await EsperarTerminadaAsync(tenantId, despacho.Value!.ExecutionId);

        // Al verse terminada, la salida ya tiene que estar entera: el buzón conserva el orden de llegada.
        Assert.Equal(ExecutionStatus.Succeeded, ejecucion.Status);
        Assert.Equal(esperado, ejecucion.StdOut);
    }

    [Fact]
    public async Task Despacho_DeUnScriptDeBashAUnServidorWindows_SeRechaza()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId), ServerPlatform.Windows);

        var scriptId = await CrearScriptAsync(tenantId, "echo hola", ScriptRuntime.Bash);
        var despacho = await DespacharAsync(tenantId, scriptId, agente.ServerId, ExecutionMode.Attached);

        Assert.True(despacho.IsFailure);
        Assert.Equal("execution.runtime_incompatible", despacho.Error!.Code);
        Assert.Equal(0, await ContarEjecucionesAsync(tenantId));
    }

    [Fact]
    public async Task Despacho_ConElAgenteDesconectado_DevuelveConflicto()
    {
        var tenantId = await CrearTenantAsync();
        Guid serverId;

        await using (var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId)))
        {
            serverId = agente.ServerId;
        }

        var scriptId = await CrearScriptAsync(tenantId, "echo hola", ScriptRuntime.Bash);
        var despacho = await EsperarRechazoPorDesconexionAsync(tenantId, scriptId, serverId);

        Assert.True(despacho.IsFailure);
        Assert.Equal("agent.not_connected", despacho.Error!.Code);
    }

    [Fact]
    public async Task Despacho_DeUnScriptInexistente_DevuelveNoEncontrado()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        var despacho = await DespacharAsync(tenantId, Guid.CreateVersion7(), agente.ServerId, ExecutionMode.Attached);

        Assert.True(despacho.IsFailure);
        Assert.Equal(ErrorKind.NotFound, despacho.Error!.Kind);
    }

    [Fact]
    public async Task Ejecucion_QueTerminaEnFallo_GuardaCodigoDeSalidaYStderr()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        var scriptId = await CrearScriptAsync(tenantId, "exit 3", ScriptRuntime.Bash);
        var despacho = await DespacharAsync(tenantId, scriptId, agente.ServerId, ExecutionMode.Attached);
        var orden = await agente.NextOrderAsync();

        await agente.ReportStartedAsync(orden.ExecutionId);
        await agente.ReportOutputAsync(orden.ExecutionId, OutputStream.Stderr, "algo salió mal\n");
        await agente.ReportCompletedAsync(orden.ExecutionId, ExecutionStatus.Failed, 3);

        var ejecucion = await EsperarTerminadaAsync(tenantId, despacho.Value!.ExecutionId);

        Assert.Equal(ExecutionStatus.Failed, ejecucion.Status);
        Assert.Equal(3, ejecucion.ExitCode);
        Assert.Equal("algo salió mal\n", ejecucion.StdErr);
        Assert.Equal(string.Empty, ejecucion.StdOut);
    }

    private async Task<Result<ScriptExecutionDispatched>> DespacharAsync(Guid tenantId, Guid scriptId, Guid serverId, ExecutionMode mode)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new DispatchScriptRequest(scriptId, serverId, mode, null, null, null);

        return await scope.ServiceProvider.GetRequiredService<IScriptDispatchService>().DispatchAsync(request, CancellationToken.None);
    }

    /// <summary>El servidor tarda un instante en marcarse como desconectado tras cerrarse el socket.</summary>
    private async Task<Result<ScriptExecutionDispatched>> EsperarRechazoPorDesconexionAsync(Guid tenantId, Guid scriptId, Guid serverId)
    {
        var despacho = await DespacharAsync(tenantId, scriptId, serverId, ExecutionMode.Attached);

        for (var intento = 0; intento < PollAttempts && despacho.IsSuccess; intento++)
        {
            await Task.Delay(PollIntervalMilliseconds);
            despacho = await DespacharAsync(tenantId, scriptId, serverId, ExecutionMode.Attached);
        }

        return despacho;
    }

    private async Task<Guid> CrearTenantAsync()
    {
        var tenant = new Tenant { Name = "Ejecuciones", Slug = $"ejecuciones-{Guid.NewGuid():N}", IdentityMode = IdentityMode.SelfManaged };

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        return tenant.Id;
    }

    private async Task<string> EmitirTokenAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var emitido = await scope.ServiceProvider.GetRequiredService<IEnrollmentTokenService>().CreateAsync(new CreateEnrollmentTokenRequest("Ejecuciones", ServerKind.Vps, null, null), CancellationToken.None);

        Assert.True(emitido.IsSuccess, emitido.Error?.Message);
        return emitido.Value!.Token;
    }

    private async Task<Guid> CrearScriptAsync(Guid tenantId, string contenido, ScriptRuntime runtime)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new CreateScriptRequest($"script-{Guid.NewGuid():N}", null, contenido, runtime, 60);
        var creado = await scope.ServiceProvider.GetRequiredService<IScriptService>().CreateAsync(request, CancellationToken.None);

        Assert.True(creado.IsSuccess, creado.Error?.Message);
        return creado.Value!.Id;
    }

    private async Task<int> ContarEjecucionesAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScriptExecutions.CountAsync();
    }

    private async Task<List<ScriptExecution>> EsperarEjecucionesTerminadasAsync(Guid tenantId, int esperadas)
    {
        var ejecuciones = new List<ScriptExecution>();

        for (var intento = 0; intento < PollAttempts; intento++)
        {
            await using (var scope = factory.CreateTenantScope(tenantId))
            {
                ejecuciones = await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScriptExecutions.AsNoTracking().ToListAsync();
            }

            if (ejecuciones.Count >= esperadas && ejecuciones.All(ejecucion => ejecucion.IsFinished))
            {
                return ejecuciones;
            }

            await Task.Delay(PollIntervalMilliseconds);
        }

        return ejecuciones;
    }

    private async Task<ScriptExecution> EsperarTerminadaAsync(Guid tenantId, Guid executionId)
    {
        ScriptExecution? ejecucion = null;

        for (var intento = 0; intento < PollAttempts; intento++)
        {
            await using (var scope = factory.CreateTenantScope(tenantId))
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

    private const int PollAttempts = 100;

    private const int PollIntervalMilliseconds = 100;
}
