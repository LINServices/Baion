using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Persistence.Context;
using Baion.Orchestrator.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ScriptUpdateTests(OrchestratorFactory factory) : IClassFixture<OrchestratorFactory>
{
    [Fact]
    public async Task Editar_CambiandoElContenido_SubeLaVersionYRecalculaElChecksum()
    {
        var tenantId = await CrearTenantAsync();
        var scriptId = await CrearScriptAsync(tenantId, "respaldo", "echo uno");

        var editado = await EditarAsync(tenantId, scriptId, new UpdateScriptRequest("respaldo", "diario", "echo dos", ScriptRuntime.Bash, 120));

        Assert.True(editado.IsSuccess, editado.Error?.Message);
        Assert.Equal(2, editado.Value!.Version);
        Assert.Equal(Sha256Hex("echo dos"), editado.Value.Checksum);

        var detalle = await DetalleAsync(tenantId, scriptId);
        Assert.Equal("echo dos", detalle.Content);
        Assert.Equal("diario", detalle.Description);
        Assert.Equal(120, detalle.DefaultTimeoutSeconds);
        Assert.NotNull(detalle.UpdatedAt);
    }

    [Fact]
    public async Task Editar_SoloLosMetadatos_NoTocaLaVersionNiElChecksum()
    {
        var tenantId = await CrearTenantAsync();
        var scriptId = await CrearScriptAsync(tenantId, "respaldo", "echo uno");
        var checksumOriginal = Sha256Hex("echo uno");

        var editado = await EditarAsync(tenantId, scriptId, new UpdateScriptRequest("respaldo-renombrado", null, "echo uno", ScriptRuntime.Bash, 90));

        Assert.True(editado.IsSuccess, editado.Error?.Message);
        Assert.Equal(1, editado.Value!.Version);
        Assert.Equal(checksumOriginal, editado.Value.Checksum);

        var detalle = await DetalleAsync(tenantId, scriptId);
        Assert.Equal("respaldo-renombrado", detalle.Name);
        Assert.Equal(90, detalle.DefaultTimeoutSeconds);
    }

    [Fact]
    public async Task Editar_ScriptInexistente_DevuelveNotFound()
    {
        var tenantId = await CrearTenantAsync();

        var editado = await EditarAsync(tenantId, Guid.CreateVersion7(), new UpdateScriptRequest("x", null, "echo x", ScriptRuntime.Bash, 60));

        Assert.True(editado.IsFailure);
        Assert.Equal("script.not_found", editado.Error!.Code);
    }

    [Fact]
    public async Task Editar_ConContenidoEnBlanco_FallaLaValidacion()
    {
        var tenantId = await CrearTenantAsync();
        var scriptId = await CrearScriptAsync(tenantId, "respaldo", "echo uno");

        var editado = await EditarAsync(tenantId, scriptId, new UpdateScriptRequest("respaldo", null, "   ", ScriptRuntime.Bash, 60));

        Assert.True(editado.IsFailure);
        Assert.Equal("script.content_required", editado.Error!.Code);
    }

    [Fact]
    public async Task Editar_ScriptDeOtroTenant_DevuelveNotFound()
    {
        var propio = await CrearTenantAsync();
        var ajeno = await CrearTenantAsync();
        var scriptAjeno = await CrearScriptAsync(ajeno, "respaldo", "echo uno");

        var editado = await EditarAsync(propio, scriptAjeno, new UpdateScriptRequest("respaldo", null, "echo dos", ScriptRuntime.Bash, 60));

        Assert.True(editado.IsFailure);
        Assert.Equal("script.not_found", editado.Error!.Code);
    }

    private async Task<Result<ScriptSummary>> EditarAsync(Guid tenantId, Guid scriptId, UpdateScriptRequest request)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<IScriptService>().UpdateAsync(scriptId, request, CancellationToken.None);
    }

    private async Task<ScriptDetail> DetalleAsync(Guid tenantId, Guid scriptId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var detalle = await scope.ServiceProvider.GetRequiredService<IScriptService>().GetDetailAsync(scriptId, CancellationToken.None);

        Assert.True(detalle.IsSuccess, detalle.Error?.Message);
        return detalle.Value!;
    }

    private async Task<Guid> CrearTenantAsync()
    {
        var tenant = new Tenant { Name = "Edicion", Slug = $"edicion-{Guid.NewGuid():N}", IdentityMode = IdentityMode.SelfManaged };

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        return tenant.Id;
    }

    private async Task<Guid> CrearScriptAsync(Guid tenantId, string nombre, string contenido)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var creado = await scope.ServiceProvider.GetRequiredService<IScriptService>().CreateAsync(new CreateScriptRequest(nombre, null, contenido, ScriptRuntime.Bash, 60), CancellationToken.None);

        Assert.True(creado.IsSuccess, creado.Error?.Message);
        return creado.Value!.Id;
    }

    // El servicio guarda el SHA-256 del contenido en hexadecimal en minúscula; aquí se replica para contrastar.
    private static string Sha256Hex(string content) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}
