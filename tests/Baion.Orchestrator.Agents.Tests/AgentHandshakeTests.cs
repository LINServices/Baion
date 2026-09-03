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
using Baion.Orchestrator.Persistence;
using Baion.Orchestrator.Persistence.Context;
using Baion.Orchestrator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class AgentHandshakeTests(OrchestratorFactory factory) : IClassFixture<OrchestratorFactory>
{
    [Theory]
    [InlineData(ServerPlatform.Linux, "linux-x64")]
    [InlineData(ServerPlatform.Windows, "win-x64")]
    public async Task Enrolamiento_RegistraElServidorConSuPlataformaYDevuelveCredencial(ServerPlatform platform, string runtimeIdentifier)
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenDeInstalacionAsync(tenantId);
        var machineId = NuevoMachineId();

        var (socket, welcome) = await ConectarAsync(enrollmentToken: token, hello: NuevoSaludo(machineId, platform, runtimeIdentifier));

        using (socket)
        {
            Assert.NotNull(welcome);
            Assert.Equal(tenantId, welcome.TenantId);
            Assert.False(string.IsNullOrWhiteSpace(welcome.AgentToken));

            var server = await ObtenerServidorAsync(tenantId, welcome.ServerId);
            Assert.Equal(platform, server.Platform);
            Assert.Equal(runtimeIdentifier, server.RuntimeIdentifier);
            Assert.Equal(machineId, server.MachineId);
            Assert.Equal(ServerStatus.Online, server.Status);
            Assert.Equal(factory.InstanceId, server.OrchestratorInstanceId);
        }
    }

    [Fact]
    public async Task Reconexion_ConLaCredencialPermanente_ReutilizaElMismoServidor()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenDeInstalacionAsync(tenantId);
        var machineId = NuevoMachineId();

        var (primerSocket, enrolamiento) = await ConectarAsync(enrollmentToken: token, hello: NuevoSaludo(machineId));
        primerSocket.Dispose();

        var (segundoSocket, reconexion) = await ConectarAsync(agentToken: enrolamiento!.AgentToken, hello: NuevoSaludo(machineId));

        using (segundoSocket)
        {
            Assert.Equal(enrolamiento.ServerId, reconexion!.ServerId);

            // La credencial solo se emite una vez: en la reconexión ya no viaja.
            Assert.Null(reconexion.AgentToken);
            Assert.Equal(1, await ContarServidoresAsync(tenantId));
        }
    }

    [Fact]
    public async Task Reconexion_TrasReiniciarLaInstancia_VuelveADejarElServidorEnLinea()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenDeInstalacionAsync(tenantId);
        var machineId = NuevoMachineId();

        var (primerSocket, enrolamiento) = await ConectarAsync(enrollmentToken: token, hello: NuevoSaludo(machineId));
        primerSocket.Dispose();

        // Simula el arranque en frío: la instancia suelta la presencia que dejó colgada al caerse.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAgentRepository>().ReleaseInstanceServersAsync(factory.InstanceId, CancellationToken.None);
        }

        var liberado = await ObtenerServidorAsync(tenantId, enrolamiento!.ServerId);
        Assert.Equal(ServerStatus.Offline, liberado.Status);
        Assert.Null(liberado.OrchestratorInstanceId);

        var (segundoSocket, reconexion) = await ConectarAsync(agentToken: enrolamiento.AgentToken, hello: NuevoSaludo(machineId));

        using (segundoSocket)
        {
            Assert.Equal(enrolamiento.ServerId, reconexion!.ServerId);

            var recuperado = await ObtenerServidorAsync(tenantId, enrolamiento.ServerId);
            Assert.Equal(ServerStatus.Online, recuperado.Status);
            Assert.Equal(factory.InstanceId, recuperado.OrchestratorInstanceId);
        }
    }

    [Fact]
    public async Task Enrolamiento_DeDosMaquinasConElMismoHostname_DesambiguaElNombre()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenDeInstalacionAsync(tenantId, maxUses: 5);

        // Mismo hostname y máquinas distintas: pasa con contenedores, VMs clonadas, o cuando la misma
        // máquina pierde su estado y vuelve a enrolarse. El nombre es único por tenant.
        const string hostname = "web-01";

        var (primerSocket, primero) = await ConectarAsync(enrollmentToken: token, hello: NuevoSaludo(NuevoMachineId()) with { Hostname = hostname });
        primerSocket.Dispose();

        var (segundoSocket, segundo) = await ConectarAsync(enrollmentToken: token, hello: NuevoSaludo(NuevoMachineId()) with { Hostname = hostname });

        using (segundoSocket)
        {
            Assert.NotNull(segundo);
            Assert.NotEqual(primero!.ServerId, segundo.ServerId);

            var uno = await ObtenerServidorAsync(tenantId, primero.ServerId);
            var dos = await ObtenerServidorAsync(tenantId, segundo.ServerId);

            Assert.Equal(hostname, uno.Name);
            Assert.StartsWith(hostname + "-", dos.Name);
            Assert.Equal(hostname, dos.Hostname);
        }
    }

    [Fact]
    public async Task Enrolamiento_DeLaMismaMaquinaDosVeces_NoDuplicaElServidor()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenDeInstalacionAsync(tenantId, maxUses: 5);
        var machineId = NuevoMachineId();

        var (primerSocket, primero) = await ConectarAsync(enrollmentToken: token, hello: NuevoSaludo(machineId));
        primerSocket.Dispose();

        var (segundoSocket, segundo) = await ConectarAsync(enrollmentToken: token, hello: NuevoSaludo(machineId));

        using (segundoSocket)
        {
            Assert.Equal(primero!.ServerId, segundo!.ServerId);
            Assert.Equal(1, await ContarServidoresAsync(tenantId));
        }
    }

    [Fact]
    public async Task Conexion_SinCredenciales_NoLlegaAAbrirElSocket()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => ConectarCrudoAsync(null, null));

        Assert.Contains(((int)HttpStatusCode.Unauthorized).ToString(), error.Message);
    }

    [Fact]
    public async Task Conexion_ConTokenRevocado_NoLlegaAAbrirElSocket()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenDeInstalacionAsync(tenantId);

        await using (var scope = factory.CreateTenantScope(tenantId))
        {
            var tokens = scope.ServiceProvider.GetRequiredService<BaionDbContext>().EnrollmentTokens;
            var almacenado = await tokens.SingleAsync();
            var revocacion = await scope.ServiceProvider.GetRequiredService<IEnrollmentTokenService>().RevokeAsync(almacenado.Id, CancellationToken.None);
            Assert.True(revocacion.IsSuccess);
        }

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => ConectarCrudoAsync(token, null));

        Assert.Contains(((int)HttpStatusCode.Unauthorized).ToString(), error.Message);
    }

    [Fact]
    public async Task Saludo_ConOtraVersionDeProtocolo_RechazaLaConexion()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenDeInstalacionAsync(tenantId);

        using var socket = await ConectarCrudoAsync(token, null);
        using var channel = new BaionMessageChannel(socket);

        var saludo = NuevoSaludo(NuevoMachineId()) with { ProtocolVersion = "0.9" };
        await channel.SendAsync<AgentToServerMessage>(saludo, TestTimeout());

        var respuesta = await channel.ReceiveAsync<ServerToAgentMessage>(TestTimeout());

        var rechazo = Assert.IsType<ConnectionRejectedMessage>(respuesta);
        Assert.Equal("agent.protocol_mismatch", rechazo.Code);
        Assert.Equal(0, await ContarServidoresAsync(tenantId));
    }

    [Fact]
    public async Task Latido_RefrescaLaUltimaSenalDeVidaDelServidor()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenDeInstalacionAsync(tenantId);

        var (socket, welcome) = await ConectarAsync(enrollmentToken: token, hello: NuevoSaludo(NuevoMachineId()));

        using (socket)
        {
            var antes = (await ObtenerServidorAsync(tenantId, welcome!.ServerId)).LastSeenAt;

            using var channel = new BaionMessageChannel(socket);
            await channel.SendAsync<AgentToServerMessage>(new HeartbeatMessage(RunningExecutions: 0), TestTimeout());

            var refrescado = await EsperarAsync(async () =>
            {
                var server = await ObtenerServidorAsync(tenantId, welcome.ServerId);
                return server.LastSeenAt > antes;
            });

            Assert.True(refrescado, "el latido no refrescó last_seen_at dentro del plazo");
        }
    }

    private async Task<(WebSocket Socket, WelcomeMessage? Welcome)> ConectarAsync(HelloMessage hello, string? enrollmentToken = null, string? agentToken = null)
    {
        var socket = await ConectarCrudoAsync(enrollmentToken, agentToken);
        var channel = new BaionMessageChannel(socket);

        await channel.SendAsync<AgentToServerMessage>(hello, TestTimeout());
        var respuesta = await channel.ReceiveAsync<ServerToAgentMessage>(TestTimeout());

        return (socket, respuesta as WelcomeMessage);
    }

    private async Task<WebSocket> ConectarCrudoAsync(string? enrollmentToken, string? agentToken)
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
        var tenant = new Tenant { Name = "Agentes", Slug = $"agentes-{Guid.NewGuid():N}", IdentityMode = IdentityMode.SelfManaged };

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        return tenant.Id;
    }

    private async Task<string> EmitirTokenDeInstalacionAsync(Guid tenantId, int? maxUses = null)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new CreateEnrollmentTokenRequest("Instalación de pruebas", ServerKind.Vps, null, maxUses);
        var emitido = await scope.ServiceProvider.GetRequiredService<IEnrollmentTokenService>().CreateAsync(request, CancellationToken.None);

        Assert.True(emitido.IsSuccess, emitido.Error?.Message);
        return emitido.Value!.Token;
    }

    private async Task<Server> ObtenerServidorAsync(Guid tenantId, Guid serverId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Servers.AsNoTracking().SingleAsync(server => server.Id == serverId);
    }

    private async Task<int> ContarServidoresAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Servers.CountAsync();
    }

    private static HelloMessage NuevoSaludo(string machineId, ServerPlatform platform = ServerPlatform.Linux, string runtimeIdentifier = "linux-x64") =>
        new(BaionProtocol.Version, platform, runtimeIdentifier, "1.0.0", $"host-{machineId[..8]}", machineId, 4, 8_000_000_000);

    private static string NuevoMachineId() => Guid.NewGuid().ToString("N");

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

    private static CancellationToken TestTimeout() => new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token;

    private const int PollAttempts = 50;

    private const int PollIntervalMilliseconds = 100;
}
