using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using LIN.Cloud.Enterprise.Baion.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Tests;

public class GroupIsolationTests(BaionDatabaseFixture fixture) : IClassFixture<BaionDatabaseFixture>
{
    [Fact]
    public async Task Consulta_DeGruposYMiembros_SoloDevuelveLosDelTenantActivo()
    {
        var primero = await CrearTenantConGrupoAsync();
        var segundo = await CrearTenantConGrupoAsync();

        await using var scope = fixture.CreateScope(primero.TenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        Assert.Equal(primero.GroupId, Assert.Single(await context.ServerGroups.ToListAsync()).Id);
        Assert.Equal(primero.GroupId, Assert.Single(await context.ServerGroupMembers.ToListAsync()).ServerGroupId);
        Assert.DoesNotContain(await context.ServerGroups.ToListAsync(), group => group.Id == segundo.GroupId);
    }

    [Fact]
    public async Task Consulta_DeLotes_SoloDevuelveLosDelTenantActivo()
    {
        var primero = await CrearTenantConLoteAsync();
        var segundo = await CrearTenantConLoteAsync();

        await using var scope = fixture.CreateScope(primero.TenantId);
        var lotes = await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ExecutionBatches.ToListAsync();

        Assert.Equal(primero.BatchId, Assert.Single(lotes).Id);
        Assert.DoesNotContain(lotes, batch => batch.Id == segundo.BatchId);
    }

    [Fact]
    public async Task Consulta_SinTenantEnElScope_NoDevuelveGruposNiLotes()
    {
        await CrearTenantConLoteAsync();

        await using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        Assert.Empty(await context.ServerGroups.ToListAsync());
        Assert.Empty(await context.ServerGroupMembers.ToListAsync());
        Assert.Empty(await context.ExecutionBatches.ToListAsync());
    }

    [Fact]
    public async Task GetWithMembersAsync_ConGrupoDeOtroTenant_DevuelveNull()
    {
        var primero = await CrearTenantConGrupoAsync();
        var segundo = await CrearTenantConGrupoAsync();

        await using var scope = fixture.CreateScope(primero.TenantId);
        var repositorio = scope.ServiceProvider.GetRequiredService<IServerGroupRepository>();

        Assert.Null(await repositorio.GetWithMembersAsync(segundo.GroupId));
        Assert.NotNull(await repositorio.GetWithMembersAsync(primero.GroupId));
    }

    [Fact]
    public async Task ConsultasDeGruposYLotes_ConIdDeOtroTenant_DevuelvenNull()
    {
        var primero = await CrearTenantConLoteAsync();
        var segundo = await CrearTenantConLoteAsync();

        await using var scope = fixture.CreateScope(primero.TenantId);

        Assert.Null(await scope.ServiceProvider.GetRequiredService<IServerGroupQueries>().GetAsync(segundo.GroupId));
        Assert.Null(await scope.ServiceProvider.GetRequiredService<IExecutionBatchQueries>().GetAsync(segundo.BatchId));
        Assert.DoesNotContain(await scope.ServiceProvider.GetRequiredService<IServerGroupQueries>().ListAsync(), group => group.Id == segundo.GroupId);
    }

    [Fact]
    public async Task GetMemberTargetsAsync_ConGrupoDeOtroTenant_DevuelveVacio()
    {
        var primero = await CrearTenantConGrupoAsync();
        var segundo = await CrearTenantConGrupoAsync();

        await using var scope = fixture.CreateScope(primero.TenantId);
        var repositorio = scope.ServiceProvider.GetRequiredService<IServerGroupRepository>();

        Assert.Empty(await repositorio.GetMemberTargetsAsync(segundo.GroupId));
        Assert.Single(await repositorio.GetMemberTargetsAsync(primero.GroupId));
    }

    [Fact]
    public async Task GetMemberTargetsAsync_ConTenantExplicito_AcotaPorEseTenantAunSinTenantEnElScope()
    {
        var primero = await CrearTenantConGrupoAsync();
        var segundo = await CrearTenantConGrupoAsync();

        await using var scope = fixture.CreateScope();
        var repositorio = scope.ServiceProvider.GetRequiredService<IServerGroupRepository>();

        var propios = await repositorio.GetMemberTargetsAsync(primero.TenantId, primero.GroupId);
        var cruzados = await repositorio.GetMemberTargetsAsync(primero.TenantId, segundo.GroupId);

        Assert.Equal(primero.ServerId, Assert.Single(propios).ServerId);
        Assert.Empty(cruzados);
    }

    [Fact]
    public async Task Insercion_DeGrupoYLoteSinTenantIdExplicito_SellaElTenantDelScope()
    {
        var tenantId = await CrearTenantAsync();
        var scriptId = await CrearScriptAsync(tenantId);

        await using (var scope = fixture.CreateScope(tenantId))
        {
            var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();
            var grupo = new ServerGroup { Name = "sellado" };

            context.ServerGroups.Add(grupo);
            context.ExecutionBatches.Add(new ExecutionBatch { ServerGroup = grupo, ServerGroupName = grupo.Name, ScriptId = scriptId, ScriptName = "script", Mode = ExecutionMode.Attached });
            await context.SaveChangesAsync();
        }

        await using var verificacion = fixture.CreateScope(tenantId);
        var consulta = verificacion.ServiceProvider.GetRequiredService<BaionDbContext>();

        Assert.Equal(tenantId, (await consulta.ServerGroups.SingleAsync()).TenantId);
        Assert.Equal(tenantId, (await consulta.ExecutionBatches.SingleAsync()).TenantId);
    }

    [Fact]
    public async Task Insercion_DeGrupoConTenantIdAjeno_Falla()
    {
        var propio = await CrearTenantAsync();
        var ajeno = await CrearTenantAsync();

        await using var scope = fixture.CreateScope(propio);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();
        context.ServerGroups.Add(new ServerGroup { TenantId = ajeno, Name = "intruso" });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        Assert.Contains(ajeno.ToString(), error.Message);
    }

    [Fact]
    public async Task Insercion_DeLoteConTenantIdAjeno_Falla()
    {
        var propio = await CrearTenantAsync();
        var ajeno = await CrearTenantAsync();
        var scriptId = await CrearScriptAsync(propio);

        await using var scope = fixture.CreateScope(propio);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();
        context.ExecutionBatches.Add(new ExecutionBatch { TenantId = ajeno, ScriptId = scriptId, ScriptName = "script", Mode = ExecutionMode.Attached });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        Assert.Contains(ajeno.ToString(), error.Message);
    }

    [Fact]
    public async Task Insercion_DeMiembroConTenantIdAjeno_Falla()
    {
        var propio = await CrearTenantConGrupoAsync();
        var ajeno = await CrearTenantAsync();

        await using var scope = fixture.CreateScope(propio.TenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();
        context.ServerGroupMembers.Add(new ServerGroupMember { TenantId = ajeno, ServerGroupId = propio.GroupId, ServerId = propio.ServerId });

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Insercion_DeGrupoYLoteSinTenantEnElScope_Falla()
    {
        var tenantId = await CrearTenantAsync();
        var scriptId = await CrearScriptAsync(tenantId);

        await using var sinTenantGrupo = fixture.CreateScope();
        sinTenantGrupo.ServiceProvider.GetRequiredService<BaionDbContext>().ServerGroups.Add(new ServerGroup { Name = "huerfano" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => sinTenantGrupo.ServiceProvider.GetRequiredService<BaionDbContext>().SaveChangesAsync());

        await using var sinTenantLote = fixture.CreateScope();
        sinTenantLote.ServiceProvider.GetRequiredService<BaionDbContext>().ExecutionBatches.Add(new ExecutionBatch { ScriptId = scriptId, ScriptName = "script", Mode = ExecutionMode.Attached });

        await Assert.ThrowsAsync<InvalidOperationException>(() => sinTenantLote.ServiceProvider.GetRequiredService<BaionDbContext>().SaveChangesAsync());
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

    private async Task<Guid> CrearScriptAsync(Guid tenantId)
    {
        var script = new Script { Name = $"script-{Guid.NewGuid():N}", Content = "echo hola", Checksum = new string('a', 64), Runtime = ScriptRuntime.Bash };

        await using var scope = fixture.CreateScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Scripts.Add(script);
        await context.SaveChangesAsync(CancellationToken.None);

        return script.Id;
    }

    private async Task<(Guid TenantId, Guid GroupId, Guid ServerId)> CrearTenantConGrupoAsync()
    {
        var tenantId = await CrearTenantAsync();
        var servidor = new Server { Name = $"srv-{Guid.NewGuid():N}", Hostname = "10.0.0.1", Kind = ServerKind.Vps, Platform = ServerPlatform.Linux, Status = ServerStatus.Provisioning, MachineId = Guid.NewGuid().ToString("N") };
        var grupo = new ServerGroup { Name = $"grupo-{Guid.NewGuid():N}" };

        await using var scope = fixture.CreateScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Servers.Add(servidor);
        context.ServerGroups.Add(grupo);
        context.ServerGroupMembers.Add(new ServerGroupMember { ServerGroup = grupo, Server = servidor });
        await context.SaveChangesAsync();

        return (tenantId, grupo.Id, servidor.Id);
    }

    private async Task<(Guid TenantId, Guid GroupId, Guid BatchId)> CrearTenantConLoteAsync()
    {
        var (tenantId, groupId, _) = await CrearTenantConGrupoAsync();
        var scriptId = await CrearScriptAsync(tenantId);
        var lote = new ExecutionBatch { ServerGroupId = groupId, ServerGroupName = "grupo", ScriptId = scriptId, ScriptName = "script", Mode = ExecutionMode.Attached };

        await using var scope = fixture.CreateScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.ExecutionBatches.Add(lote);
        await context.SaveChangesAsync();

        return (tenantId, groupId, lote.Id);
    }
}
