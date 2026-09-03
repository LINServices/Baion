using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Baion.Orchestrator.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Baion.Orchestrator.Presentacion;

/// <summary>Registro de dependencias de la capa de presentación.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registra los controllers, filtros y la autenticación por token de la API.</summary>
    public static IServiceCollection AddPresentacion(this IServiceCollection services, IConfiguration configuration)
    {
        // Mismos enums en texto que el protocolo del agente: la API no habla con números mágicos.
        services.AddControllers().AddJsonOptions(json => json.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        services.AddAuthorization();

        var identity = configuration.GetSection(BaionIdentityOptions.SectionName).Get<BaionIdentityOptions>() ?? new BaionIdentityOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            // Sin mapeo de claims: los nombres del token de Baion llegan tal cual (tid, stamp, roles).
            options.MapInboundClaims = false;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = identity.Issuer,
                ValidAudience = identity.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(identity.SigningKey)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                NameClaimType = BaionClaims.Email,
                RoleClaimType = BaionClaims.Roles
            };
        });

        return services;
    }
}
