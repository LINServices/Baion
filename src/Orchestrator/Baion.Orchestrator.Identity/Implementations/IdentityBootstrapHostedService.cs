using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Models.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Orchestrator.Identity.Implementations;

/// <summary>
/// Crea el tenant y el usuario administrador iniciales si la configuración los declara y aún no existen.
/// No aborta el arranque si falla: deja constancia en el log y la aplicación sigue sirviendo.
/// </summary>
internal class IdentityBootstrapHostedService(IServiceScopeFactory scopeFactory, IOptions<IdentityBootstrapOptions> options, ILogger<IdentityBootstrapHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;

        if (!settings.Enabled)
        {
            return;
        }

        try
        {
            await BootstrapAsync(settings, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo completar el arranque inicial de identidad");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task BootstrapAsync(IdentityBootstrapOptions settings, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var tenantRequest = new CreateTenantRequest(settings.TenantName, settings.TenantSlug, IdentityMode.SelfManaged, null);
        var tenant = await scope.ServiceProvider.GetRequiredService<ITenantProvisioningService>().EnsureTenantAsync(tenantRequest, cancellationToken);

        if (tenant is not { IsSuccess: true, Value: Guid tenantId })
        {
            logger.LogError("Arranque inicial detenido: no se pudo asegurar el tenant '{TenantSlug}' ({Error})", settings.TenantSlug, tenant.Error?.Message);
            return;
        }

        var userRequest = new CreateUserRequest(settings.AdminEmail, settings.AdminDisplayName, settings.AdminPassword, [settings.AdminRole]);
        var user = await scope.ServiceProvider.GetRequiredService<IUserProvisioningService>().CreateUserAsync(tenantId, userRequest, cancellationToken);

        if (user.IsSuccess)
        {
            logger.LogWarning("Arranque inicial: creado el administrador {AdminEmail} en el tenant {TenantId}. Rota su contraseña cuanto antes.", settings.AdminEmail, tenantId);
            return;
        }

        // El conflicto es el caso normal a partir del segundo arranque: el administrador ya existe.
        if (user.Error is { Kind: ErrorKind.Conflict })
        {
            logger.LogDebug("Arranque inicial: el administrador ya existía en el tenant {TenantId}", tenantId);
            return;
        }

        logger.LogError("Arranque inicial: no se pudo crear el administrador ({Error})", user.Error?.Message);
    }
}
