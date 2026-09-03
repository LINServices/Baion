using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Contracts.Enums;
using Baion.Contracts.Messages;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Persistence.Context;
using Baion.Orchestrator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Desactivación forzada de un servidor. Se comprueba sobre sockets de verdad, porque lo que hay que
/// verificar es justamente que al agente se le avisa y se le echa sin esperar a que colabore.
/// </summary>
public class ServerDeactivationTests(OrchestratorFactory factory) : IClassFixture<OrchestratorFactory>
{
    [Fact]
    public async Task Desactivar_ConElAgenteConectado_LeAvisaDelMotivoYLeCierraElSocket()
    {
        var tenantId = await CrearTenantAsync();
        var (socket, channel, welcome) = await ConectarAsync(await EmitirTokenAsync(tenantId));

        using (socket)
        using (channel)
        {
            var desactivacion = await DesactivarAsync(tenantId, welcome.ServerId);

            Assert.True(desactivacion.IsSuccess, desactivacion.Error?.Message);
            Assert.Equal(ServerStatus.Disabled, desactivacion.Value!.Status);

            var aviso = Assert.IsType<ConnectionRejectedMessage>(await channel.ReceiveAsync<ServerToAgentMessage>(TestTimeout()));
            Assert.Equal("agent.server_disabled", aviso.Code);

            // Tras el aviso cierra el orquestador: el canal se queda sin nada que leer aunque el agente
            // no haya colgado. Eso es lo que hace que la desactivación sea forzada y no una petición.
            Assert.Null(await channel.ReceiveAsync<ServerToAgentMessage>(TestTimeout()));
        }
    }

    [Fact]
    public async Task Desactivar_NoQuedaDeshechoPorElCierreDelSocket()
    {
        var tenantId = await CrearTenantAsync();
        var (socket, channel, welcome) = await ConectarAsync(await EmitirTokenAsync(tenantId));

        using (socket)
        using (channel)
        {
            Assert.True((await DesactivarAsync(tenantId, welcome.ServerId)).IsSuccess);
        }

        // El cierre dispara la baja de presencia, que es quien deja los servidores en Offline. Un servidor
        // desactivado no puede volver por ahí: sería readmitirlo sin que nadie lo haya reactivado.
        var soltado = await EsperarAsync(async () => (await ObtenerServidorAsync(tenantId, welcome.ServerId)).OrchestratorInstanceId is null);
        Assert.True(soltado, "el orquestador no soltó la presencia del servidor desactivado");

        var servidor = await ObtenerServidorAsync(tenantId, welcome.ServerId);
        Assert.Equal(ServerStatus.Disabled, servidor.Status);
        Assert.Null(servidor.ConnectedAt);
    }

    [Fact]
    public async Task Reconexion_DeUnServidorDesactivado_NoLlegaAAbrirElSocket()
    {
        var tenantId = await CrearTenantAsync();
        var (socket, channel, welcome) = await ConectarAsync(await EmitirTokenAsync(tenantId));

        socket.Dispose();
        channel.Dispose();

        Assert.True((await DesactivarAsync(tenantId, welcome.ServerId)).IsSuccess);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => ConectarCrudoAsync(agentToken: welcome.AgentToken));

        Assert.Contains(((int)HttpStatusCode.Forbidden).ToString(), error.Message);
    }

    [Fact]
    public async Task Reactivar_DevuelveElServidorAOffline_YSuAgenteVuelveAEntrar()
    {
        var tenantId = await CrearTenantAsync();
        var machineId = NuevoMachineId();
        var (socket, channel, welcome) = await ConectarAsync(await EmitirTokenAsync(tenantId), machineId);

        socket.Dispose();
        channel.Dispose();

        Assert.True((await DesactivarAsync(tenantId, welcome.ServerId)).IsSuccess);

        var reactivacion = await ReactivarAsync(tenantId, welcome.ServerId);

        Assert.True(reactivacion.IsSuccess, reactivacion.Error?.Message);

        // Offline y no Online: quien lo pone en línea es el saludo del agente, con la versión que traiga.
        Assert.Equal(ServerStatus.Offline, reactivacion.Value!.Status);

        // La credencial nunca se tocó, así que el agente entra con la que ya tenía sin volver a enrolarse.
        var (reconexion, canalDeVuelta, bienvenida) = await ConectarAsync(machineId: machineId, agentToken: welcome.AgentToken);

        using (reconexion)
        using (canalDeVuelta)
        {
            Assert.Equal(welcome.ServerId, bienvenida.ServerId);
            Assert.Equal(ServerStatus.Online, (await ObtenerServidorAsync(tenantId, welcome.ServerId)).Status);
        }
    }

    [Fact]
    public async Task Despacho_AUnServidorDesactivado_SeRechazaSinCrearLaEjecucion()
    {
        var tenantId = await CrearTenantAsync();
        await using var agente = await FakeAgent.ConnectAsync(factory, await EmitirTokenAsync(tenantId));

        var scriptId = await CrearScriptAsync(tenantId, "echo hola");
        Assert.True((await DesactivarAsync(tenantId, agente.ServerId)).IsSuccess);

        var despacho = await DespacharAsync(tenantId, scriptId, agente.ServerId);

        Assert.False(despacho.IsSuccess);
        Assert.Equal("server.disabled", despacho.Error!.Code);
        Assert.Equal(ErrorKind.Conflict, despacho.Error.Kind);
        Assert.Equal(0, await ContarEjecucionesAsync(tenantId));
    }

    private async Task<Result<ServerSummary>> DesactivarAsync(Guid tenantId, Guid serverId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<IServerService>().DisableAsync(serverId, CancellationToken.None);
    }

    private async Task<Result<ServerSummary>> ReactivarAsync(Guid tenantId, Guid serverId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<IServerService>().EnableAsync(serverId, CancellationToken.None);
    }

    private async Task<Result<ScriptExecutionDispatched>> DespacharAsync(Guid tenantId, Guid scriptId, Guid serverId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new DispatchScriptRequest(scriptId, serverId, ExecutionMode.Attached, null, null, null);

        return await scope.ServiceProvider.GetRequiredService<IScriptDispatchService>().DispatchAsync(request, CancellationToken.None);
    }

    private async Task<(WebSocket Socket, BaionMessageChannel Channel, WelcomeMessage Welcome)> ConectarAsync(string? enrollmentToken = null, string? machineId = null, string? agentToken = null)
    {
        var socket = await ConectarCrudoAsync(enrollmentToken, agentToken);
        var channel = new BaionMessageChannel(socket);

        machineId ??= NuevoMachineId();
        var hello = new HelloMessage(BaionProtocol.Version, ServerPlatform.Linux, "linux-x64", "1.0.0", $"host-{machineId[..8]}", machineId, 4, 8_000_000_000);

        await channel.SendAsync<AgentToServerMessage>(hello, TestTimeout());
        var welcome = Assert.IsType<WelcomeMessage>(await channel.ReceiveAsync<ServerToAgentMessage>(TestTimeout()));

        return (socket, channel, welcome);
    }

    private async Task<WebSocket> ConectarCrudoAsync(string? enrollmentToken = null, string? agentToken = null)
    {
        var client = factory.Server.CreateWebSocketClient();

        client.ConfigureRequest = request =>
        {
            request.Headers[BaionProtocol.ProtocolVersionHeader] = BaionProtocol.Version;

            if (enrollmentToken is not null)
            {
                request.Headers[BaionProtocol.EnrollmentTokenHeader] = enrollmentToken;
            }

            if (agentToken is not null)
            {
                request.Headers[BaionProtocol.AgentTokenHeader] = agentToken;
            }
        };

        return await client.ConnectAsync(new Uri(factory.Server.BaseAddress, BaionProtocol.WebSocketPath.TrimStart('/')), TestTimeout());
    }

    private async Task<Guid> CrearTenantAsync()
    {
        var tenant = new Tenant { Name = "Desactivacion", Slug = $"desactivacion-{Guid.NewGuid():N}", IdentityMode = IdentityMode.SelfManaged };

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        return tenant.Id;
    }

    private async Task<string> EmitirTokenAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new CreateEnrollmentTokenRequest("Instalacion de pruebas", ServerKind.Vps, null, null);
        var emitido = await scope.ServiceProvider.GetRequiredService<IEnrollmentTokenService>().CreateAsync(request, CancellationToken.None);

        Assert.True(emitido.IsSuccess, emitido.Error?.Message);
        return emitido.Value!.Token;
    }

    private async Task<Guid> CrearScriptAsync(Guid tenantId, string contenido)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new CreateScriptRequest($"script-{Guid.NewGuid():N}", null, contenido, ScriptRuntime.Bash, 60);
        var creado = await scope.ServiceProvider.GetRequiredService<IScriptService>().CreateAsync(request, CancellationToken.None);

        Assert.True(creado.IsSuccess, creado.Error?.Message);
        return creado.Value!.Id;
    }

    private async Task<Server> ObtenerServidorAsync(Guid tenantId, Guid serverId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Servers.AsNoTracking().SingleAsync(server => server.Id == serverId);
    }

    private async Task<int> ContarEjecucionesAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScriptExecutions.CountAsync();
    }

    /// <summary>Sondea una condición que el servidor cumple de forma asíncrona, sin dormir a ciegas.</summary>
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

    private static string NuevoMachineId() => Guid.NewGuid().ToString("N");

    private static CancellationToken TestTimeout() => new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token;

    private const int PollAttempts = 50;

    private const int PollIntervalMilliseconds = 100;
}
