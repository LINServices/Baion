using System;
using System.Text;
using Baion.Orchestrator.Identity.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Baion.Orchestrator.Identity;

/// <summary>Registro de dependencias de la capa.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registra el proveedor de identidad según la configuración del tenant.</summary>
    public static IServiceCollection AddIdentityProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BaionIdentityOptions>()
            .Bind(configuration.GetSection(BaionIdentityOptions.SectionName))
            .Validate(HasUsableSigningKey, $"La sección '{BaionIdentityOptions.SectionName}' necesita un SigningKey de al menos {MinimumSigningKeyBytes} bytes.")
            .ValidateOnStart();

        services.AddOptions<IdentityBootstrapOptions>().Bind(configuration.GetSection(IdentityBootstrapOptions.SectionName));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        // Ambos proveedores se registran; AuthenticationService elige por el IdentityMode del tenant.
        services.AddScoped<IIdentityProvider, SelfManagedIdentityProvider>();
        services.AddScoped<IIdentityProvider, LinIdentityProvider>();

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();

        services.AddHostedService<IdentityBootstrapHostedService>();

        return services;
    }

    private static bool HasUsableSigningKey(BaionIdentityOptions options) => !string.IsNullOrWhiteSpace(options.SigningKey) && Encoding.UTF8.GetByteCount(options.SigningKey) >= MinimumSigningKeyBytes;

    /// <summary>HMAC-SHA256 exige una clave de al menos el tamaño del hash.</summary>
    private const int MinimumSigningKeyBytes = 32;
}
