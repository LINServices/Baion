using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Contracts.Enums;
using Baion.Contracts.Messages;
using Baion.Contracts.Metrics;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Persistence.Context;
using Baion.Orchestrator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class MetricIngestTests(OrchestratorFactory factory) : IClassFixture<OrchestratorFactory>
{
    [Fact]
    public async Task Metricas_DeAgentesLinuxYWindowsConcurrentes_SeInsertanTodas()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenAsync(tenantId);

        var linux = await ConectarAsync(token, ServerPlatform.Linux, "linux-x64");
        var windows = await ConectarAsync(token, ServerPlatform.Windows, "win-x64");

        using (linux.Socket)
        using (windows.Socket)
        {
            const int muestrasPorAgente = 20;

            // Los dos agentes empujan a la vez: es la situación que el buzón tiene que absorber.
            await Task.WhenAll(
                EnviarMuestrasAsync(linux.Channel, muestrasPorAgente, cpu: 25),
                EnviarMuestrasAsync(windows.Channel, muestrasPorAgente, cpu: 75));

            var total = await EsperarConteoAsync(tenantId, muestrasPorAgente * 2);
            Assert.Equal(muestrasPorAgente * 2, total);

            var porServidor = await ContarPorServidorAsync(tenantId);
            Assert.Equal(muestrasPorAgente, porServidor[linux.ServerId]);
            Assert.Equal(muestrasPorAgente, porServidor[windows.ServerId]);
        }
    }

    [Fact]
    public async Task Metricas_SePersistenConCpuMemoriaYVolumenes()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenAsync(tenantId);
        var agente = await ConectarAsync(token, ServerPlatform.Linux, "linux-x64");

        using (agente.Socket)
        {
            await agente.Channel.SendAsync<AgentToServerMessage>(NuevaMuestra(cpu: 42.5), TestTimeout());

            await EsperarConteoAsync(tenantId, 1);
            var metrica = await LeerUnicaMetricaAsync(tenantId);

            Assert.Equal(agente.ServerId, metrica.ServerId);
            Assert.Equal(42.5, metrica.CpuUsagePercent);
            Assert.Equal(8, metrica.CpuCoreCount);
            Assert.Equal(16_000_000_000, metrica.MemoryTotalBytes);
            Assert.Equal("/", Assert.Single(metrica.Disks).MountPoint);
        }
    }

    [Fact]
    public async Task Encolar_NoBloqueaElHiloDelSocket()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenAsync(tenantId);
        var agente = await ConectarAsync(token, ServerPlatform.Linux, "linux-x64");

        using (agente.Socket)
        {
            const int muestras = 100;
            var cronometro = Stopwatch.StartNew();
            await EnviarMuestrasAsync(agente.Channel, muestras, cpu: 10);
            cronometro.Stop();

            // Si el socket esperase por cada INSERT, 100 mensajes no volverían en este orden de magnitud.
            Assert.True(cronometro.ElapsedMilliseconds < 2000, $"enviar {muestras} muestras tardó {cronometro.ElapsedMilliseconds} ms");
            Assert.Equal(muestras, await EsperarConteoAsync(tenantId, muestras));
        }
    }

    [Fact]
    public async Task Metricas_RefrescanLaUltimaSenalDeVidaDelServidor()
    {
        var tenantId = await CrearTenantAsync();
        var token = await EmitirTokenAsync(tenantId);
        var agente = await ConectarAsync(token, ServerPlatform.Linux, "linux-x64");

        using (agente.Socket)
        {
            var capturada = DateTimeOffset.UtcNow.AddMinutes(1);
            await agente.Channel.SendAsync<AgentToServerMessage>(NuevaMuestra(cpu: 5) with { CapturedAt = capturada }, TestTimeout());

            await EsperarConteoAsync(tenantId, 1);

            var refrescado = await EsperarAsync(async () =>
            {
                await using var scope = factory.CreateTenantScope(tenantId);
                var server = await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Servers.AsNoTracking().SingleAsync(candidate => candidate.Id == agente.ServerId);
                return server.LastSeenAt >= capturada;
            });

            Assert.True(refrescado, "la métrica no refrescó last_seen_at");
        }
    }

    [Fact]
    public async Task TablaDeMetricas_EstaParticionadaPorMes()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        var indicesParticionados = await ContarEscalarAsync(context, """
            SELECT COUNT(*)
            FROM sys.indexes i
            JOIN sys.partition_schemes ps ON i.data_space_id = ps.data_space_id
            WHERE i.object_id = OBJECT_ID('metrics') AND ps.name = 'ps_metrics_monthly'
            """);

        var limites = await ContarEscalarAsync(context, """
            SELECT COUNT(*)
            FROM sys.partition_range_values prv
            JOIN sys.partition_functions pf ON prv.function_id = pf.function_id
            WHERE pf.name = 'pf_metrics_monthly'
            """);

        Assert.Equal(2, indicesParticionados);
        Assert.True(limites >= 1, "la función de partición no tiene límites");
    }

    private async Task<(WebSocket Socket, BaionMessageChannel Channel, Guid ServerId)> ConectarAsync(string enrollmentToken, ServerPlatform platform, string runtimeIdentifier)
    {
        var client = factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers[BaionProtocol.EnrollmentTokenHeader] = enrollmentToken;

        var socket = await client.ConnectAsync(new Uri(factory.Server.BaseAddress, BaionProtocol.WebSocketPath.TrimStart('/')), TestTimeout());
        var channel = new BaionMessageChannel(socket);

        var machineId = Guid.NewGuid().ToString("N");
        var hello = new HelloMessage(BaionProtocol.Version, platform, runtimeIdentifier, "1.0.0", $"host-{machineId[..8]}", machineId, 8, 16_000_000_000);

        await channel.SendAsync<AgentToServerMessage>(hello, TestTimeout());
        var welcome = Assert.IsType<WelcomeMessage>(await channel.ReceiveAsync<ServerToAgentMessage>(TestTimeout()));

        return (socket, channel, welcome.ServerId);
    }

    private static async Task EnviarMuestrasAsync(BaionMessageChannel channel, int cantidad, double cpu)
    {
        for (var indice = 0; indice < cantidad; indice++)
        {
            await channel.SendAsync<AgentToServerMessage>(NuevaMuestra(cpu), TestTimeout());
        }
    }

    private static MetricsReportMessage NuevaMuestra(double cpu) => new(
        DateTimeOffset.UtcNow,
        new CpuMetrics(cpu, 8, 0.5),
        new MemoryMetrics(16_000_000_000, 4_000_000_000),
        [new DiskMetrics("sda1", "/", 500_000_000_000, 200_000_000_000)]);

    private async Task<Guid> CrearTenantAsync()
    {
        var tenant = new Tenant { Name = "Métricas", Slug = $"metricas-{Guid.NewGuid():N}", IdentityMode = IdentityMode.SelfManaged };

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        return tenant.Id;
    }

    private async Task<string> EmitirTokenAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var emitido = await scope.ServiceProvider.GetRequiredService<IEnrollmentTokenService>().CreateAsync(new CreateEnrollmentTokenRequest("Métricas", ServerKind.Vps, null, null), CancellationToken.None);

        Assert.True(emitido.IsSuccess, emitido.Error?.Message);
        return emitido.Value!.Token;
    }

    private async Task<int> EsperarConteoAsync(Guid tenantId, int esperado)
    {
        var ultimo = 0;

        await EsperarAsync(async () =>
        {
            await using var scope = factory.CreateTenantScope(tenantId);
            ultimo = await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Metrics.CountAsync();
            return ultimo >= esperado;
        });

        return ultimo;
    }

    private async Task<Dictionary<Guid, int>> ContarPorServidorAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);

        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Metrics
            .GroupBy(metric => metric.ServerId)
            .Select(grupo => new { grupo.Key, Total = grupo.Count() })
            .ToDictionaryAsync(fila => fila.Key, fila => fila.Total);
    }

    private async Task<Metric> LeerUnicaMetricaAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Metrics.AsNoTracking().SingleAsync();
    }

    private static async Task<int> ContarEscalarAsync(BaionDbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();

        if (connection.State is not System.Data.ConnectionState.Open)
        {
            await context.Database.OpenConnectionAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        return Convert.ToInt32(await command.ExecuteScalarAsync());
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

    private static CancellationToken TestTimeout() => new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token;

    private const int PollAttempts = 100;

    private const int PollIntervalMilliseconds = 100;
}
