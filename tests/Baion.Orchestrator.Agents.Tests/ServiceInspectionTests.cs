using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Contracts.Messages;
using Baion.Contracts.Services;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Persistence.Context;
using Baion.Orchestrator.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// La inspección de servicios es pass-through: el orquestador pregunta al agente por el socket en el
/// momento y traduce su respuesta. Estas pruebas usan <see cref="FakeAgent"/> para fijar qué contesta.
/// </summary>
public class ServiceInspectionTests(OrchestratorFactory factory) : IClassFixture<OrchestratorFactory>
{
    [Fact]
    public async Task Listado_ConAgenteConectado_DevuelveLosServiciosDelAgente()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        agente.QueryResponder = mensaje => mensaje switch
        {
            ListServicesRequestMessage peticion => new ServicesListedMessage(peticion.RequestId,
            [
                new ServiceSummary("nginx.service", "Servidor web nginx", ServiceRuntimeState.Running, "active (running)", ServiceStartupMode.Automatic),
                new ServiceSummary("cron.service", "Demonio cron", ServiceRuntimeState.Stopped, "inactive (dead)", ServiceStartupMode.Manual),
            ]),
            _ => null
        };

        var resultado = await ConInspeccionAsync(tenantId, servicio => servicio.ListAsync(agente.ServerId, null, CancellationToken.None));

        Assert.True(resultado.IsSuccess, resultado.Error?.Message);
        Assert.Collection(resultado.Value!,
            servicio => Assert.Equal("nginx.service", servicio.Id),
            servicio => Assert.Equal("cron.service", servicio.Id));
    }

    [Fact]
    public async Task Listado_PasaElFiltroDeNombreAlAgente_YRecortaLosEspacios()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        string? filtroRecibido = null;
        agente.QueryResponder = mensaje =>
        {
            if (mensaje is ListServicesRequestMessage peticion)
            {
                filtroRecibido = peticion.NameFilter;
                return new ServicesListedMessage(peticion.RequestId, []);
            }

            return null;
        };

        await ConInspeccionAsync(tenantId, servicio => servicio.ListAsync(agente.ServerId, "  ngin  ", CancellationToken.None));

        Assert.Equal("ngin", filtroRecibido);
    }

    [Fact]
    public async Task Detalle_CuandoElAgenteResponde_DevuelveElDetalle()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId), ServerPlatform.Windows);

        agente.QueryResponder = mensaje => mensaje switch
        {
            DescribeServiceRequestMessage peticion => new ServiceDescribedMessage(peticion.RequestId, Detalle(peticion.ServiceId)),
            _ => null
        };

        var resultado = await ConInspeccionAsync(tenantId, servicio => servicio.DescribeAsync(agente.ServerId, "Spooler", CancellationToken.None));

        Assert.True(resultado.IsSuccess, resultado.Error?.Message);
        Assert.Equal("Spooler", resultado.Value!.Id);
        Assert.Equal(ServiceRuntimeState.Running, resultado.Value.State);
    }

    [Fact]
    public async Task Detalle_CuandoElAgenteDiceQueNoExiste_DevuelveNotFound()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        agente.QueryResponder = mensaje => mensaje switch
        {
            DescribeServiceRequestMessage peticion => new ServiceQueryFailedMessage(peticion.RequestId, ServiceQueryFailedMessage.NotFoundCode, "El servicio fantasma.service no existe."),
            _ => null
        };

        var resultado = await ConInspeccionAsync(tenantId, servicio => servicio.DescribeAsync(agente.ServerId, "fantasma.service", CancellationToken.None));

        Assert.True(resultado.IsFailure);
        Assert.Equal("service.not_found", resultado.Error!.Code);
    }

    [Fact]
    public async Task Logs_RecortaElNumeroDeLineasPedidoAlTope()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        var pedidas = new List<int>();
        agente.QueryResponder = mensaje =>
        {
            if (mensaje is ServiceLogsRequestMessage peticion)
            {
                pedidas.Add(peticion.MaxLines);
                return new ServiceLogsMessage(peticion.RequestId, [new ServiceLogLine(DateTimeOffset.UtcNow, "info", "arranca")], Truncated: true);
            }

            return null;
        };

        var conTope = await ConInspeccionAsync(tenantId, servicio => servicio.GetLogsAsync(agente.ServerId, "nginx.service", 999_999, null, CancellationToken.None));
        var porDefecto = await ConInspeccionAsync(tenantId, servicio => servicio.GetLogsAsync(agente.ServerId, "nginx.service", null, null, CancellationToken.None));

        Assert.True(conTope.IsSuccess, conTope.Error?.Message);
        Assert.True(conTope.Value!.Truncated);
        Assert.Equal([2000, 200], pedidas);
        Assert.True(porDefecto.IsSuccess, porDefecto.Error?.Message);
    }

    [Fact]
    public async Task Control_CuandoElAgenteAplicaLaAccion_DevuelveElDetalleResultante()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        ServiceControlAction? accionRecibida = null;
        agente.QueryResponder = mensaje =>
        {
            if (mensaje is ControlServiceRequestMessage peticion)
            {
                accionRecibida = peticion.Action;
                return new ServiceControlledMessage(peticion.RequestId, Detalle(peticion.ServiceId));
            }

            return null;
        };

        var resultado = await ConInspeccionAsync(tenantId, servicio => servicio.ControlAsync(agente.ServerId, "nginx.service", ServiceControlAction.Restart, CancellationToken.None));

        Assert.True(resultado.IsSuccess, resultado.Error?.Message);
        Assert.Equal(ServiceControlAction.Restart, accionRecibida);
    }

    [Fact]
    public async Task Consulta_SinAgenteEnEstaInstancia_DevuelveConflict()
    {
        var tenantId = await CrearTenantAsync();
        var serverId = await CrearServidorSueltoAsync(tenantId);

        var resultado = await ConInspeccionAsync(tenantId, servicio => servicio.ListAsync(serverId, null, CancellationToken.None));

        Assert.True(resultado.IsFailure);
        Assert.Equal("agent.not_reachable", resultado.Error!.Code);
    }

    [Fact]
    public async Task Consulta_ConServidorInexistente_DevuelveNotFound()
    {
        var tenantId = await CrearTenantAsync();

        var resultado = await ConInspeccionAsync(tenantId, servicio => servicio.ListAsync(Guid.CreateVersion7(), null, CancellationToken.None));

        Assert.True(resultado.IsFailure);
        Assert.Equal("server.not_found", resultado.Error!.Code);
    }

    [Fact]
    public async Task Consulta_CuandoElAgenteNoResponde_VenceElPlazoYDevuelveConflict()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        // Sin responder: el mensaje llega al agente y se queda sin contestar.
        agente.QueryResponder = _ => null;

        var resultado = await ConInspeccionAsync(tenantId, servicio => servicio.DescribeAsync(agente.ServerId, "nginx.service", CancellationToken.None));

        Assert.True(resultado.IsFailure);
        Assert.Equal("agent.query_timeout", resultado.Error!.Code);
    }

    private static ServiceDetail Detalle(string serviceId) => new(
        serviceId,
        serviceId,
        "Servicio de prueba",
        ServiceRuntimeState.Running,
        "active (running)",
        "running",
        ServiceStartupMode.Automatic,
        1234,
        DateTimeOffset.UtcNow.AddHours(-1),
        10_000_000,
        $"/lib/systemd/system/{serviceId}",
        null,
        ["network.target"]);

    private async Task<T> ConInspeccionAsync<T>(Guid tenantId, Func<IServiceInspectionService, Task<T>> trabajo)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await trabajo(scope.ServiceProvider.GetRequiredService<IServiceInspectionService>());
    }

    private async Task<Guid> CrearTenantAsync()
    {
        var tenant = new Tenant { Name = "Servicios", Slug = $"servicios-{Guid.NewGuid():N}", IdentityMode = IdentityMode.SelfManaged };

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        return tenant.Id;
    }

    private async Task<string> EmitirTokenAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var emitido = await scope.ServiceProvider.GetRequiredService<IEnrollmentTokenService>().CreateAsync(new CreateEnrollmentTokenRequest("Servicios", ServerKind.Vps, null, null), CancellationToken.None);

        Assert.True(emitido.IsSuccess, emitido.Error?.Message);
        return emitido.Value!.Token;
    }

    private async Task<Guid> CrearServidorSueltoAsync(Guid tenantId)
    {
        var server = new Server
        {
            Name = "servidor-sin-agente",
            Hostname = "servidor-sin-agente.local",
            Kind = ServerKind.Vps,
            Platform = ServerPlatform.Linux,
            Status = ServerStatus.Offline,
            MachineId = Guid.NewGuid().ToString("N")
        };

        await using var scope = factory.CreateTenantScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Servers.Add(server);
        await context.SaveChangesAsync();

        return server.Id;
    }
}
