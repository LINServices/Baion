using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Persistence.Context;
using Baion.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Baion.Orchestrator.Persistence.Tests;

public class AislamientoMultiTenantTests(BaionDatabaseFixture fixture) : IClassFixture<BaionDatabaseFixture>
{
    [Fact]
    public async Task Consulta_SoloDevuelveFilasDelTenantActivo()
    {
        var (primero, segundo) = await CrearDosTenantsConServidorAsync();

        await using var scope = fixture.CreateScope(primero);
        var servidores = await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Servers.ToListAsync();

        Assert.Equal(primero, Assert.Single(servidores).TenantId);
        Assert.DoesNotContain(servidores, server => server.TenantId == segundo);
    }

    [Fact]
    public async Task GetByIdAsync_ConIdDeOtroTenant_DevuelveNull()
    {
        var (primero, segundo) = await CrearDosTenantsConServidorAsync();

        Guid ajeno;
        await using (var scopeSegundo = fixture.CreateScope(segundo))
        {
            ajeno = (await scopeSegundo.ServiceProvider.GetRequiredService<BaionDbContext>().Servers.SingleAsync()).Id;
        }

        await using var scopePrimero = fixture.CreateScope(primero);
        var encontrado = await scopePrimero.ServiceProvider.GetRequiredService<IRepository<Server>>().GetByIdAsync(ajeno);

        Assert.Null(encontrado);
    }

    [Fact]
    public async Task Insercion_SinTenantIdExplicito_SellaElTenantDelScope()
    {
        var tenantId = await CrearTenantAsync();

        await using (var scope = fixture.CreateScope(tenantId))
        {
            await scope.ServiceProvider.GetRequiredService<IRepository<Script>>().AddAsync(NuevoScript());
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        }

        await using var verificacion = fixture.CreateScope(tenantId);
        var script = await verificacion.ServiceProvider.GetRequiredService<BaionDbContext>().Scripts.SingleAsync();

        Assert.Equal(tenantId, script.TenantId);
        Assert.NotEqual(default, script.CreatedAt);
    }

    [Fact]
    public async Task Insercion_ConTenantIdAjeno_Falla()
    {
        var propio = await CrearTenantAsync();
        var ajeno = await CrearTenantAsync();

        await using var scope = fixture.CreateScope(propio);
        await scope.ServiceProvider.GetRequiredService<IRepository<Script>>().AddAsync(NuevoScript(ajeno));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None));

        Assert.Contains(ajeno.ToString(), error.Message);
    }

    [Fact]
    public async Task Insercion_SinTenantEnElScope_Falla()
    {
        await using var scope = fixture.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IRepository<Script>>().AddAsync(NuevoScript());

        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Metricas_PersistenLosVolumenesComoJsonEnLaMismaFila()
    {
        var (tenantId, serverId) = await CrearTenantConServidorAsync();

        await using (var scope = fixture.CreateScope(tenantId))
        {
            var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();
            context.Metrics.Add(new Metric
            {
                ServerId = serverId,
                CapturedAt = DateTimeOffset.UtcNow,
                CpuUsagePercent = 37.5,
                CpuCoreCount = 8,
                LoadAverage1m = 0.9,
                MemoryTotalBytes = 16_000_000_000,
                MemoryAvailableBytes = 4_000_000_000,
                Disks = [new MetricDisk { Name = "sda1", MountPoint = "/", TotalBytes = 500_000_000_000, AvailableBytes = 125_000_000_000 }]
            });

            await context.SaveChangesAsync();
        }

        await using var verificacion = fixture.CreateScope(tenantId);
        var metrica = await verificacion.ServiceProvider.GetRequiredService<BaionDbContext>().Metrics.SingleAsync();

        Assert.Equal(tenantId, metrica.TenantId);
        Assert.Equal("/", Assert.Single(metrica.Disks).MountPoint);
    }

    private async Task<Guid> CrearTenantAsync()
    {
        var tenant = new Tenant { Name = $"Tenant {Guid.NewGuid():N}", Slug = $"tenant-{Guid.NewGuid():N}", IdentityMode = IdentityMode.SelfManaged };

        await using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        return tenant.Id;
    }

    private async Task<(Guid TenantId, Guid ServerId)> CrearTenantConServidorAsync()
    {
        var tenantId = await CrearTenantAsync();
        var server = NuevoServidor();

        await using var scope = fixture.CreateScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Servers.Add(server);
        await context.SaveChangesAsync();

        return (tenantId, server.Id);
    }

    private async Task<(Guid Primero, Guid Segundo)> CrearDosTenantsConServidorAsync()
    {
        var (primero, _) = await CrearTenantConServidorAsync();
        var (segundo, _) = await CrearTenantConServidorAsync();
        return (primero, segundo);
    }

    private static Server NuevoServidor() => new()
    {
        Name = $"srv-{Guid.NewGuid():N}",
        Hostname = "10.0.0.1",
        Kind = ServerKind.Vps,
        Platform = ServerPlatform.Linux,
        Status = ServerStatus.Provisioning
    };

    private static Script NuevoScript(Guid? tenantId = null) => new()
    {
        TenantId = tenantId ?? Guid.Empty,
        Name = $"script-{Guid.NewGuid():N}",
        Content = "echo hola",
        Checksum = new string('a', 64),
        Runtime = ScriptRuntime.Bash
    };
}
