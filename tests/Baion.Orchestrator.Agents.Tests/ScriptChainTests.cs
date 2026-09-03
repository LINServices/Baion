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

public class ScriptChainTests(OrchestratorFactory factory) : IClassFixture<OrchestratorFactory>
{
    [Fact]
    public async Task CadenaDeTresPasos_TodosCorrectos_SeEjecutanEnOrden()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        var pasos = await CrearScriptsAsync(tenantId, 3);
        var chainId = await CrearCadenaAsync(tenantId, pasos, ChainFailurePolicy.StopChain);

        var recorrido = await ArrancarAsync(tenantId, chainId, agente.ServerId);
        Assert.True(recorrido.IsSuccess, recorrido.Error?.Message);

        var ejecutados = new List<Guid>();

        for (var indice = 0; indice < 3; indice++)
        {
            var orden = await agente.NextOrderAsync();
            ejecutados.Add(orden.ExecutionId);

            await agente.ReportStartedAsync(orden.ExecutionId);
            await agente.ReportOutputAsync(orden.ExecutionId, OutputStream.Stdout, $"paso-{indice}\n");
            await agente.ReportCompletedAsync(orden.ExecutionId, ExecutionStatus.Succeeded, 0);
        }

        var detalle = await EsperarRecorridoAsync(tenantId, recorrido.Value!.ChainRunId, ChainRunStatus.Succeeded);

        Assert.Equal(ChainRunStatus.Succeeded, detalle.Status);
        Assert.Equal([1, 2, 3], detalle.Steps.Select(paso => paso.Order));
        Assert.All(detalle.Steps, paso => Assert.Equal(ExecutionStatus.Succeeded, paso.Status));

        // El orden real de despacho: cada paso solo sale cuando el anterior ya terminó.
        Assert.Equal(ejecutados, detalle.Steps.Select(paso => paso.ExecutionId!.Value));
    }

    [Fact]
    public async Task CadenaDeTresPasos_ConFalloIntermedioYPoliticaDeParada_NoLanzaElTercero()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        var pasos = await CrearScriptsAsync(tenantId, 3);
        var chainId = await CrearCadenaAsync(tenantId, pasos, ChainFailurePolicy.StopChain);

        var recorrido = await ArrancarAsync(tenantId, chainId, agente.ServerId);

        var primero = await agente.NextOrderAsync();
        await agente.ReportCompletedAsync(primero.ExecutionId, ExecutionStatus.Succeeded, 0);

        var segundo = await agente.NextOrderAsync();
        await agente.ReportOutputAsync(segundo.ExecutionId, OutputStream.Stderr, "reventó\n");
        await agente.ReportCompletedAsync(segundo.ExecutionId, ExecutionStatus.Failed, 1);

        var detalle = await EsperarRecorridoAsync(tenantId, recorrido.Value!.ChainRunId, ChainRunStatus.Stopped);

        Assert.Equal(ChainRunStatus.Stopped, detalle.Status);
        Assert.Equal(ExecutionStatus.Succeeded, detalle.Steps[0].Status);
        Assert.Equal(ExecutionStatus.Failed, detalle.Steps[1].Status);

        // El tercero nunca llegó a existir: la política cortó la cadena.
        Assert.Null(detalle.Steps[2].ExecutionId);
        Assert.Equal(2, await ContarEjecucionesAsync(tenantId));
    }

    [Fact]
    public async Task CadenaDeTresPasos_ConFalloIntermedioYPoliticaDeContinuar_LlegaHastaElFinal()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        var pasos = await CrearScriptsAsync(tenantId, 3);
        var chainId = await CrearCadenaAsync(tenantId, pasos, ChainFailurePolicy.ContinueNext);

        var recorrido = await ArrancarAsync(tenantId, chainId, agente.ServerId);

        var primero = await agente.NextOrderAsync();
        await agente.ReportCompletedAsync(primero.ExecutionId, ExecutionStatus.Succeeded, 0);

        var segundo = await agente.NextOrderAsync();
        await agente.ReportCompletedAsync(segundo.ExecutionId, ExecutionStatus.Failed, 1);

        var tercero = await agente.NextOrderAsync();
        await agente.ReportCompletedAsync(tercero.ExecutionId, ExecutionStatus.Succeeded, 0);

        var detalle = await EsperarRecorridoAsync(tenantId, recorrido.Value!.ChainRunId, ChainRunStatus.CompletedWithFailures);

        Assert.Equal(ChainRunStatus.CompletedWithFailures, detalle.Status);
        Assert.All(detalle.Steps, paso => Assert.NotNull(paso.ExecutionId));
        Assert.Equal(ExecutionStatus.Failed, detalle.Steps[1].Status);
    }

    [Fact]
    public async Task DosCadenasEnParalelo_AvanzanSinMezclarSusPasos()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        var pasos = await CrearScriptsAsync(tenantId, 2);
        var chainId = await CrearCadenaAsync(tenantId, pasos, ChainFailurePolicy.StopChain);

        var primera = await ArrancarAsync(tenantId, chainId, agente.ServerId);
        var segunda = await ArrancarAsync(tenantId, chainId, agente.ServerId);

        Assert.True(primera.IsSuccess, primera.Error?.Message);
        Assert.True(segunda.IsSuccess, segunda.Error?.Message);
        Assert.NotEqual(primera.Value!.ChainRunId, segunda.Value!.ChainRunId);

        // Los dos primeros pasos salen a la vez: el agente los encola en su semáforo, no el orquestador.
        var iniciales = new[] { await agente.NextOrderAsync(), await agente.NextOrderAsync() };
        Assert.Equal(2, iniciales.Select(orden => orden.ExecutionId).Distinct().Count());

        foreach (var orden in iniciales)
        {
            await agente.ReportCompletedAsync(orden.ExecutionId, ExecutionStatus.Succeeded, 0);
        }

        var siguientes = new[] { await agente.NextOrderAsync(), await agente.NextOrderAsync() };

        foreach (var orden in siguientes)
        {
            await agente.ReportCompletedAsync(orden.ExecutionId, ExecutionStatus.Succeeded, 0);
        }

        var detallePrimera = await EsperarRecorridoAsync(tenantId, primera.Value.ChainRunId, ChainRunStatus.Succeeded);
        var detalleSegunda = await EsperarRecorridoAsync(tenantId, segunda.Value.ChainRunId, ChainRunStatus.Succeeded);

        Assert.Equal(ChainRunStatus.Succeeded, detallePrimera.Status);
        Assert.Equal(ChainRunStatus.Succeeded, detalleSegunda.Status);

        var ejecucionesPrimera = detallePrimera.Steps.Select(paso => paso.ExecutionId).ToList();
        Assert.DoesNotContain(detalleSegunda.Steps[0].ExecutionId, ejecucionesPrimera);
        Assert.DoesNotContain(detalleSegunda.Steps[1].ExecutionId, ejecucionesPrimera);
    }

    [Fact]
    public async Task Arranque_SiUnPasoNoEsCompatibleConLaPlataforma_SeRechazaLaCadenaEntera()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId), ServerPlatform.Windows);

        // El primer paso sí correría en Windows; el segundo no. Se rechaza antes de lanzar nada.
        var compatible = await CrearScriptAsync(tenantId, ScriptRuntime.PowerShellCore);
        var incompatible = await CrearScriptAsync(tenantId, ScriptRuntime.Bash);
        var chainId = await CrearCadenaAsync(tenantId, [compatible, incompatible], ChainFailurePolicy.StopChain);

        var recorrido = await ArrancarAsync(tenantId, chainId, agente.ServerId);

        Assert.True(recorrido.IsFailure);
        Assert.Equal("chain.runtime_incompatible", recorrido.Error!.Code);
        Assert.Equal(0, await ContarEjecucionesAsync(tenantId));
    }

    [Fact]
    public async Task Alta_ConDosPasosEnLaMismaPosicion_SeRechaza()
    {
        var tenantId = await CrearTenantAsync();
        var script = await CrearScriptAsync(tenantId, ScriptRuntime.Bash);

        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new CreateScriptChainRequest("colisión", null,
        [
            new CreateScriptChainStepRequest(script, 1, ChainFailurePolicy.StopChain, null),
            new CreateScriptChainStepRequest(script, 1, ChainFailurePolicy.StopChain, null)
        ]);

        var creada = await scope.ServiceProvider.GetRequiredService<IScriptChainService>().CreateAsync(request, CancellationToken.None);

        Assert.True(creada.IsFailure);
        Assert.Equal("chain.duplicated_order", creada.Error!.Code);
    }

    [Fact]
    public async Task Avance_SiElDesenlaceSeProcesaDosVeces_NoLanzaElPasoSiguienteDosVeces()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        var pasos = await CrearScriptsAsync(tenantId, 2);
        var chainId = await CrearCadenaAsync(tenantId, pasos, ChainFailurePolicy.StopChain);

        var recorrido = await ArrancarAsync(tenantId, chainId, agente.ServerId);
        var primero = await agente.NextOrderAsync();

        await agente.ReportCompletedAsync(primero.ExecutionId, ExecutionStatus.Succeeded, 0);
        var segundo = await agente.NextOrderAsync();

        // Se fuerza un segundo avance sobre el mismo paso, como si el desenlace se reprocesara.
        await using (var scope = factory.CreateTenantScope(tenantId))
        {
            await scope.ServiceProvider.GetRequiredService<IScriptChainService>().AdvanceAsync(primero.ExecutionId, CancellationToken.None);
        }

        await agente.ReportCompletedAsync(segundo.ExecutionId, ExecutionStatus.Succeeded, 0);
        await EsperarRecorridoAsync(tenantId, recorrido.Value!.ChainRunId, ChainRunStatus.Succeeded);

        Assert.Equal(2, await ContarEjecucionesAsync(tenantId));
    }

    private async Task<Result<ScriptChainRunStarted>> ArrancarAsync(Guid tenantId, Guid chainId, Guid serverId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<IScriptChainService>().StartAsync(new StartChainRequest(chainId, serverId), CancellationToken.None);
    }

    private async Task<ScriptChainRunDetail> EsperarRecorridoAsync(Guid tenantId, Guid chainRunId, ChainRunStatus esperado)
    {
        Result<ScriptChainRunDetail> detalle;

        for (var intento = 0; intento < PollAttempts; intento++)
        {
            await using (var scope = factory.CreateTenantScope(tenantId))
            {
                detalle = await scope.ServiceProvider.GetRequiredService<IScriptChainService>().GetRunAsync(chainRunId, CancellationToken.None);
            }

            if (detalle is { IsSuccess: true, Value: ScriptChainRunDetail actual } && actual.Status == esperado)
            {
                return actual;
            }

            await Task.Delay(PollIntervalMilliseconds);
        }

        await using var ultimo = factory.CreateTenantScope(tenantId);
        var final = await ultimo.ServiceProvider.GetRequiredService<IScriptChainService>().GetRunAsync(chainRunId, CancellationToken.None);

        Assert.True(final.IsSuccess, final.Error?.Message);
        return final.Value!;
    }

    private async Task<Guid> CrearTenantAsync()
    {
        var tenant = new Tenant { Name = "Cadenas", Slug = $"cadenas-{Guid.NewGuid():N}", IdentityMode = IdentityMode.SelfManaged };

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        return tenant.Id;
    }

    private async Task<string> EmitirTokenAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var emitido = await scope.ServiceProvider.GetRequiredService<IEnrollmentTokenService>().CreateAsync(new CreateEnrollmentTokenRequest("Cadenas", ServerKind.Vps, null, null), CancellationToken.None);

        Assert.True(emitido.IsSuccess, emitido.Error?.Message);
        return emitido.Value!.Token;
    }

    private async Task<Guid> CrearScriptAsync(Guid tenantId, ScriptRuntime runtime)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new CreateScriptRequest($"script-{Guid.NewGuid():N}", null, "echo paso", runtime, 60);
        var creado = await scope.ServiceProvider.GetRequiredService<IScriptService>().CreateAsync(request, CancellationToken.None);

        Assert.True(creado.IsSuccess, creado.Error?.Message);
        return creado.Value!.Id;
    }

    private async Task<List<Guid>> CrearScriptsAsync(Guid tenantId, int cantidad)
    {
        var creados = new List<Guid>();

        for (var indice = 0; indice < cantidad; indice++)
        {
            creados.Add(await CrearScriptAsync(tenantId, ScriptRuntime.Bash));
        }

        return creados;
    }

    private async Task<Guid> CrearCadenaAsync(Guid tenantId, IReadOnlyList<Guid> scriptIds, ChainFailurePolicy policy)
    {
        await using var scope = factory.CreateTenantScope(tenantId);

        var pasos = scriptIds.Select((scriptId, indice) => new CreateScriptChainStepRequest(scriptId, indice + 1, policy, null)).ToList();
        var creada = await scope.ServiceProvider.GetRequiredService<IScriptChainService>().CreateAsync(new CreateScriptChainRequest($"cadena-{Guid.NewGuid():N}", null, pasos), CancellationToken.None);

        Assert.True(creada.IsSuccess, creada.Error?.Message);
        return creada.Value!.Id;
    }

    private async Task<int> ContarEjecucionesAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScriptExecutions.CountAsync();
    }

    private const int PollAttempts = 100;

    private const int PollIntervalMilliseconds = 100;
}
