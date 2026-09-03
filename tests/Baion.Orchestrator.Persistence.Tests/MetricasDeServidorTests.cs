using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Persistence.Context;
using Baion.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Baion.Orchestrator.Persistence.Tests;

public class MetricasDeServidorTests(BaionDatabaseFixture fixture) : IClassFixture<BaionDatabaseFixture>
{
    [Fact]
    public async Task ListMetricsAsync_DevuelveLasMuestrasDeLaMasRecienteALaMasAntiguaConSusVolumenes()
    {
        var (tenantId, serverId) = await CrearTenantConServidorAsync();
        var baseInstante = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        await SembrarMetricasAsync(tenantId, serverId, baseInstante, muestras: 3);

        await using var scope = fixture.CreateScope(tenantId);
        var pagina = await scope.ServiceProvider.GetRequiredService<IServerQueries>()
            .ListMetricsAsync(serverId, new MetricsWindow(null, null), page: 1, pageSize: 25, CancellationToken.None);

        Assert.Equal(3, pagina.TotalCount);
        Assert.Collection(pagina.Items,
            primera => Assert.Equal(baseInstante.AddMinutes(2), primera.CapturedAt),
            segunda => Assert.Equal(baseInstante.AddMinutes(1), segunda.CapturedAt),
            tercera => Assert.Equal(baseInstante, tercera.CapturedAt));
        Assert.Equal("/", Assert.Single(pagina.Items[0].Disks).MountPoint);
    }

    [Fact]
    public async Task ListMetricsAsync_AcotaPorLaVentanaTemporal()
    {
        var (tenantId, serverId) = await CrearTenantConServidorAsync();
        var baseInstante = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        await SembrarMetricasAsync(tenantId, serverId, baseInstante, muestras: 5);

        await using var scope = fixture.CreateScope(tenantId);
        var pagina = await scope.ServiceProvider.GetRequiredService<IServerQueries>()
            .ListMetricsAsync(serverId, new MetricsWindow(baseInstante.AddMinutes(1), baseInstante.AddMinutes(4)), page: 1, pageSize: 25, CancellationToken.None);

        Assert.Equal(3, pagina.TotalCount);
        Assert.All(pagina.Items, muestra => Assert.InRange(muestra.CapturedAt, baseInstante.AddMinutes(1), baseInstante.AddMinutes(3)));
    }

    [Fact]
    public async Task ListMetricsAsync_NoVeLasMetricasDeOtroTenant()
    {
        var (propio, servidorPropio) = await CrearTenantConServidorAsync();
        var (ajeno, servidorAjeno) = await CrearTenantConServidorAsync();
        var baseInstante = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        await SembrarMetricasAsync(ajeno, servidorAjeno, baseInstante, muestras: 2);

        await using var scope = fixture.CreateScope(propio);
        var pagina = await scope.ServiceProvider.GetRequiredService<IServerQueries>()
            .ListMetricsAsync(servidorAjeno, new MetricsWindow(null, null), page: 1, pageSize: 25, CancellationToken.None);

        Assert.Empty(pagina.Items);
        Assert.Equal(0, pagina.TotalCount);
        Assert.NotEqual(servidorPropio, servidorAjeno);
    }

    private async Task SembrarMetricasAsync(Guid tenantId, Guid serverId, DateTimeOffset baseInstante, int muestras)
    {
        await using var scope = fixture.CreateScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        for (var i = 0; i < muestras; i++)
        {
            context.Metrics.Add(new Metric
            {
                ServerId = serverId,
                CapturedAt = baseInstante.AddMinutes(i),
                CpuUsagePercent = 10 + i,
                CpuCoreCount = 4,
                LoadAverage1m = 0.5,
                MemoryTotalBytes = 8_000_000_000,
                MemoryAvailableBytes = 2_000_000_000,
                Disks = [new MetricDisk { Name = "sda1", MountPoint = "/", TotalBytes = 500_000_000_000, AvailableBytes = 120_000_000_000 }]
            });
        }

        await context.SaveChangesAsync();
    }

    private async Task<(Guid TenantId, Guid ServerId)> CrearTenantConServidorAsync()
    {
        var tenant = new Tenant { Name = $"Tenant {Guid.NewGuid():N}", Slug = $"tenant-{Guid.NewGuid():N}", IdentityMode = IdentityMode.SelfManaged };

        await using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
        }

        var server = new Server
        {
            Name = $"srv-{Guid.NewGuid():N}",
            Hostname = "10.0.0.1",
            Kind = ServerKind.Vps,
            Platform = ServerPlatform.Linux,
            Status = ServerStatus.Provisioning
        };

        await using (var scope = fixture.CreateScope(tenant.Id))
        {
            var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();
            context.Servers.Add(server);
            await context.SaveChangesAsync();
        }

        return (tenant.Id, server.Id);
    }
}
