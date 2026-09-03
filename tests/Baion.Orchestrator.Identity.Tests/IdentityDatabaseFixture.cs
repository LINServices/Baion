using System.Collections.Generic;
using Baion.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baion.Orchestrator.Identity.Tests;

/// <summary>Añade la capa de identidad al fixture de base de datos compartido.</summary>
public class IdentityDatabaseFixture : BaionDatabaseFixture
{
    protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration) => services.AddLogging().AddIdentityProvider(configuration);

    protected override IEnumerable<KeyValuePair<string, string?>> AdditionalSettings =>
    [
        new($"{BaionIdentityOptions.SectionName}:Issuer", Issuer),
        new($"{BaionIdentityOptions.SectionName}:Audience", Audience),
        new($"{BaionIdentityOptions.SectionName}:SigningKey", SigningKey),
        new($"{BaionIdentityOptions.SectionName}:AccessTokenMinutes", "30"),
        new($"{BaionIdentityOptions.SectionName}:MaxFailedAccessAttempts", "3"),
        new($"{BaionIdentityOptions.SectionName}:LockoutMinutes", "15")
    ];

    public const string Issuer = "baion-tests";

    public const string Audience = "baion-tests";

    public const string SigningKey = "clave-de-pruebas-suficientemente-larga-para-hmac-sha256";
}
