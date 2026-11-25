using System;
using System.Text;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Identity.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Identity;

public static class ServiceCollectionExtensions
{
    // HMAC-SHA256 exige una clave de al menos el tamaño del hash.
    private const int MinimumSigningKeyBytes = 32;

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

    private static bool HasUsableSigningKey(BaionIdentityOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.SigningKey)
            && Encoding.UTF8.GetByteCount(options.SigningKey) >= MinimumSigningKeyBytes;
    }
}
