using System;
using System.Collections.Generic;
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

public class ScriptQueryTests(OrchestratorFactory factory) : IClassFixture<OrchestratorFactory>
{
    [Fact]
    public async Task ListadoDeScripts_SinBusqueda_DevuelveLosDelTenantOrdenadosPorNombre()
    {
        var tenantId = await CrearTenantAsync();
        await CrearScriptAsync(tenantId, "gamma");
        await CrearScriptAsync(tenantId, "alfa");
        await CrearScriptAsync(tenantId, "beta");

        var pagina = await ListarScriptsAsync(tenantId, null, 1, 25);

        Assert.Equal(3, pagina.TotalCount);
        Assert.Equal(["alfa", "beta", "gamma"], pagina.Items.Select(script => script.Name));
        Assert.Equal(1, pagina.TotalPages);
        Assert.False(pagina.HasPrevious);
        Assert.False(pagina.HasNext);
    }

    [Fact]
    public async Task ListadoDeScripts_ConBusqueda_SoloDevuelveLosQueLaContienenEnElNombre()
    {
        var tenantId = await CrearTenantAsync();
        await CrearScriptAsync(tenantId, "respaldo-diario");
        await CrearScriptAsync(tenantId, "respaldo-semanal");
        await CrearScriptAsync(tenantId, "limpieza-de-logs");

        var pagina = await ListarScriptsAsync(tenantId, "respaldo", 1, 25);

        Assert.Equal(2, pagina.TotalCount);
        Assert.All(pagina.Items, script => Assert.Contains("respaldo", script.Name));
    }

    [Fact]
    public async Task ListadoDeScripts_ConBusquedaEnBlanco_NoFiltra()
    {
        var tenantId = await CrearTenantAsync();
        await CrearScriptAsync(tenantId, "alfa");
        await CrearScriptAsync(tenantId, "beta");

        var pagina = await ListarScriptsAsync(tenantId, "   ", 1, 25);

        Assert.Equal(2, pagina.TotalCount);
    }

    [Fact]
    public async Task ListadoDeScripts_SegundaPagina_DevuelveSuTramoYElTotalCompleto()
    {
        var tenantId = await CrearTenantAsync();

        foreach (var nombre in Nombres)
        {
            await CrearScriptAsync(tenantId, nombre);
        }

        var pagina = await ListarScriptsAsync(tenantId, null, 2, 2);

        // El total cuenta las cinco, no las de la página: es lo que necesita el paginador.
        Assert.Equal(5, pagina.TotalCount);
        Assert.Equal(3, pagina.TotalPages);
        Assert.Equal(2, pagina.Page);
        Assert.Equal(["script-c", "script-d"], pagina.Items.Select(script => script.Name));
        Assert.True(pagina.HasPrevious);
        Assert.True(pagina.HasNext);
    }

    [Fact]
    public async Task ListadoDeScripts_ConUnTamanoDePaginaDesmedido_SeRecortaAlTope()
    {
        var tenantId = await CrearTenantAsync();
        await CrearScriptAsync(tenantId, "alfa");

        var pagina = await ListarScriptsAsync(tenantId, null, 0, 5_000);

        Assert.Equal(Pagination.FirstPage, pagina.Page);
        Assert.Equal(Pagination.MaxPageSize, pagina.PageSize);
    }

    [Fact]
    public async Task DetalleDeScript_DevuelveElContenidoYFallaSiNoExiste()
    {
        var tenantId = await CrearTenantAsync();
        var scriptId = await CrearScriptAsync(tenantId, "alfa", "echo hola");

        await using var scope = factory.CreateTenantScope(tenantId);
        var scripts = scope.ServiceProvider.GetRequiredService<IScriptService>();

        var detalle = await scripts.GetDetailAsync(scriptId, CancellationToken.None);
        var inexistente = await scripts.GetDetailAsync(Guid.CreateVersion7(), CancellationToken.None);

        Assert.True(detalle.IsSuccess, detalle.Error?.Message);
        Assert.Equal("echo hola", detalle.Value!.Content);
        Assert.True(detalle.Value.IsActive);
        Assert.True(inexistente.IsFailure);
        Assert.Equal("script.not_found", inexistente.Error!.Code);
    }

    [Fact]
    public async Task ListadoDeEjecuciones_FiltradoPorServidor_SoloDevuelveLasDeEseServidor()
    {
        var tenantId = await CrearTenantAsync();
        var scriptId = await CrearScriptAsync(tenantId, "alfa");
        var primero = await CrearServidorAsync(tenantId, "servidor-uno");
        var segundo = await CrearServidorAsync(tenantId, "servidor-dos");

        await CrearEjecucionAsync(tenantId, primero, scriptId, ExecutionStatus.Succeeded, Origen);
        await CrearEjecucionAsync(tenantId, primero, scriptId, ExecutionStatus.Failed, Origen.AddMinutes(1));
        await CrearEjecucionAsync(tenantId, segundo, scriptId, ExecutionStatus.Succeeded, Origen.AddMinutes(2));

        var pagina = await ListarEjecucionesAsync(tenantId, new ExecutionFilter(primero, null, null, null), 1, 25);

        Assert.Equal(2, pagina.TotalCount);
        Assert.All(pagina.Items, ejecucion => Assert.Equal(primero, ejecucion.ServerId));
        Assert.All(pagina.Items, ejecucion => Assert.Equal("servidor-uno", ejecucion.ServerName));
        Assert.All(pagina.Items, ejecucion => Assert.Equal("alfa", ejecucion.ScriptName));
    }

    [Fact]
    public async Task ListadoDeEjecuciones_FiltradoPorEstado_SoloDevuelveLasDeEseEstado()
    {
        var tenantId = await CrearTenantAsync();
        var scriptId = await CrearScriptAsync(tenantId, "alfa");
        var serverId = await CrearServidorAsync(tenantId, "servidor-uno");

        await CrearEjecucionAsync(tenantId, serverId, scriptId, ExecutionStatus.Succeeded, Origen);
        await CrearEjecucionAsync(tenantId, serverId, scriptId, ExecutionStatus.Failed, Origen.AddMinutes(1));
        await CrearEjecucionAsync(tenantId, serverId, scriptId, ExecutionStatus.Failed, Origen.AddMinutes(2));

        var pagina = await ListarEjecucionesAsync(tenantId, new ExecutionFilter(null, null, ExecutionStatus.Failed, null), 1, 25);

        Assert.Equal(2, pagina.TotalCount);
        Assert.All(pagina.Items, ejecucion => Assert.Equal(ExecutionStatus.Failed, ejecucion.Status));
    }

    [Fact]
    public async Task ListadoDeEjecuciones_ConVentanaTemporal_DejaFueraLasAnteriores()
    {
        var tenantId = await CrearTenantAsync();
        var scriptId = await CrearScriptAsync(tenantId, "alfa");
        var serverId = await CrearServidorAsync(tenantId, "servidor-uno");

        await CrearEjecucionAsync(tenantId, serverId, scriptId, ExecutionStatus.Succeeded, Origen);
        await CrearEjecucionAsync(tenantId, serverId, scriptId, ExecutionStatus.Succeeded, Origen.AddHours(2));

        var pagina = await ListarEjecucionesAsync(tenantId, new ExecutionFilter(null, null, null, Origen.AddHours(1)), 1, 25);

        var ejecucion = Assert.Single(pagina.Items);
        Assert.Equal(Origen.AddHours(2), ejecucion.QueuedAt);
    }

    [Fact]
    public async Task ListadoDeEjecuciones_DevuelveLoUltimoPrimeroYPaginaSinRepetir()
    {
        var tenantId = await CrearTenantAsync();
        var scriptId = await CrearScriptAsync(tenantId, "alfa");
        var serverId = await CrearServidorAsync(tenantId, "servidor-uno");

        for (var minuto = 0; minuto < 5; minuto++)
        {
            await CrearEjecucionAsync(tenantId, serverId, scriptId, ExecutionStatus.Succeeded, Origen.AddMinutes(minuto));
        }

        var primera = await ListarEjecucionesAsync(tenantId, SinFiltro, 1, 2);
        var segunda = await ListarEjecucionesAsync(tenantId, SinFiltro, 2, 2);

        Assert.Equal([Origen.AddMinutes(4), Origen.AddMinutes(3)], primera.Items.Select(ejecucion => ejecucion.QueuedAt));
        Assert.Equal([Origen.AddMinutes(2), Origen.AddMinutes(1)], segunda.Items.Select(ejecucion => ejecucion.QueuedAt));
        Assert.Equal(5, segunda.TotalCount);
        Assert.Empty(primera.Items.Select(ejecucion => ejecucion.Id).Intersect(segunda.Items.Select(ejecucion => ejecucion.Id)));
    }

    [Fact]
    public async Task ListadoDeEjecuciones_DeUnaEjecucionConSalida_NoDevuelveLaSalidaPeroElDetalleSi()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        var scriptId = await CrearScriptAsync(tenantId, "alfa", "echo hola");
        var despacho = await DespacharAsync(tenantId, scriptId, agente.ServerId);
        var orden = await agente.NextOrderAsync();

        await agente.ReportStartedAsync(orden.ExecutionId);
        await agente.ReportOutputAsync(orden.ExecutionId, OutputStream.Stdout, "hola\n");
        await agente.ReportCompletedAsync(orden.ExecutionId, ExecutionStatus.Succeeded, 0);
        await EsperarTerminadaAsync(tenantId, despacho);

        var pagina = await ListarEjecucionesAsync(tenantId, SinFiltro, 1, 25);
        var listada = Assert.Single(pagina.Items);

        // La salida es nvarchar(max) y puede pesar megabytes: el listado no la trae ni la expone.
        Assert.DoesNotContain(typeof(ScriptExecutionListItem).GetProperties(), propiedad => propiedad.Name is nameof(ScriptExecutionDetail.StdOut) or nameof(ScriptExecutionDetail.StdErr));
        Assert.Equal(despacho, listada.Id);
        Assert.Equal(0, listada.ExitCode);
        Assert.Equal("alfa", listada.ScriptName);
        Assert.False(string.IsNullOrWhiteSpace(listada.ServerName));

        await using var scope = factory.CreateTenantScope(tenantId);
        var detalle = await scope.ServiceProvider.GetRequiredService<IScriptDispatchService>().GetAsync(despacho, CancellationToken.None);

        Assert.True(detalle.IsSuccess, detalle.Error?.Message);
        Assert.Equal("hola\n", detalle.Value!.StdOut);
        Assert.Equal("alfa", detalle.Value.ScriptName);
        Assert.Equal(listada.ServerName, detalle.Value.ServerName);
    }

    private async Task<PagedResult<ScriptListItem>> ListarScriptsAsync(Guid tenantId, string? search, int page, int pageSize)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<IScriptService>().ListAsync(search, page, pageSize, CancellationToken.None);
    }

    private async Task<PagedResult<ScriptExecutionListItem>> ListarEjecucionesAsync(Guid tenantId, ExecutionFilter filter, int page, int pageSize)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<IScriptDispatchService>().ListAsync(filter, page, pageSize, CancellationToken.None);
    }

    private async Task<Guid> CrearTenantAsync()
    {
        var tenant = new Tenant { Name = "Consultas", Slug = $"consultas-{Guid.NewGuid():N}", IdentityMode = IdentityMode.SelfManaged };

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        return tenant.Id;
    }

    private async Task<Guid> CrearScriptAsync(Guid tenantId, string nombre, string contenido = "echo hola")
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new CreateScriptRequest(nombre, null, contenido, ScriptRuntime.Bash, 60);
        var creado = await scope.ServiceProvider.GetRequiredService<IScriptService>().CreateAsync(request, CancellationToken.None);

        Assert.True(creado.IsSuccess, creado.Error?.Message);
        return creado.Value!.Id;
    }

    /// <summary>Servidor puesto a mano: para las consultas basta la fila, no hace falta un agente al otro lado.</summary>
    private async Task<Guid> CrearServidorAsync(Guid tenantId, string nombre)
    {
        var server = new Server
        {
            Name = nombre,
            Hostname = $"{nombre}.local",
            Kind = ServerKind.Vps,
            Platform = ServerPlatform.Linux,
            Status = ServerStatus.Online,
            MachineId = Guid.NewGuid().ToString("N")
        };

        await using var scope = factory.CreateTenantScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Servers.Add(server);
        await context.SaveChangesAsync();

        return server.Id;
    }

    /// <summary>Se insertan a mano para fijar el instante de encolado, que es lo que ordena el listado.</summary>
    private async Task CrearEjecucionAsync(Guid tenantId, Guid serverId, Guid scriptId, ExecutionStatus status, DateTimeOffset queuedAt)
    {
        var execution = new ScriptExecution
        {
            ServerId = serverId,
            ScriptId = scriptId,
            Status = status,
            Mode = ExecutionMode.Attached,
            ExitCode = status is ExecutionStatus.Succeeded ? 0 : 1,
            QueuedAt = queuedAt,
            StartedAt = queuedAt,
            CompletedAt = queuedAt.AddSeconds(1),
            StdOut = "salida de prueba",
            StdErr = string.Empty
        };

        await using var scope = factory.CreateTenantScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.ScriptExecutions.Add(execution);
        await context.SaveChangesAsync();
    }

    private async Task<string> EmitirTokenAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var emitido = await scope.ServiceProvider.GetRequiredService<IEnrollmentTokenService>().CreateAsync(new CreateEnrollmentTokenRequest("Consultas", ServerKind.Vps, null, null), CancellationToken.None);

        Assert.True(emitido.IsSuccess, emitido.Error?.Message);
        return emitido.Value!.Token;
    }

    private async Task<Guid> DespacharAsync(Guid tenantId, Guid scriptId, Guid serverId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new DispatchScriptRequest(scriptId, serverId, ExecutionMode.Attached, null, null, null);
        var despacho = await scope.ServiceProvider.GetRequiredService<IScriptDispatchService>().DispatchAsync(request, CancellationToken.None);

        Assert.True(despacho.IsSuccess, despacho.Error?.Message);
        return despacho.Value!.ExecutionId;
    }

    private async Task EsperarTerminadaAsync(Guid tenantId, Guid executionId)
    {
        for (var intento = 0; intento < PollAttempts; intento++)
        {
            await using (var scope = factory.CreateTenantScope(tenantId))
            {
                var ejecucion = await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScriptExecutions.AsNoTracking().SingleAsync(candidate => candidate.Id == executionId);

                if (ejecucion is { IsFinished: true, StdOut: "hola\n" })
                {
                    return;
                }
            }

            await Task.Delay(PollIntervalMilliseconds);
        }
    }

    private static readonly ExecutionFilter SinFiltro = new(null, null, null, null);

    private static readonly DateTimeOffset Origen = new(2026, 1, 15, 8, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyList<string> Nombres = ["script-a", "script-b", "script-c", "script-d", "script-e"];

    private const int PollAttempts = 100;

    private const int PollIntervalMilliseconds = 100;
}
