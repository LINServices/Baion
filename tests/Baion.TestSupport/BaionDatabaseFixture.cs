using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Baion.Orchestrator.Persistence;
using Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Baion.TestSupport;

/// <summary>
/// Levanta una base <c>lin_baion</c> desechable y le aplica las migraciones reales.
/// Apunta a LocalDB salvo que <c>BAION_TEST_CONNECTION</c> indique otro servidor (CI con contenedor).
/// Las capas que necesiten más servicios sobreescriben <see cref="ConfigureServices"/>.
/// </summary>
public class BaionDatabaseFixture : IAsyncLifetime
{
    private readonly string _databaseName = $"lin_baion_tests_{Guid.NewGuid():N}";

    private ServiceProvider _services = null!;

    public async Task InitializeAsync()
    {
        _services = BuildProvider(BuildConnectionString(_databaseName));

        await using var scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using (var scope = _services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Database.EnsureDeletedAsync();
        }

        await _services.DisposeAsync();
    }

    /// <summary>Abre un scope sin tenant resuelto, como el de una operación de sistema.</summary>
    public AsyncServiceScope CreateScope() => _services.CreateAsyncScope();

    /// <summary>Abre un scope ya posicionado en un tenant, como haría el middleware de la API.</summary>
    public AsyncServiceScope CreateScope(Guid tenantId)
    {
        var scope = _services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(tenantId);
        return scope;
    }

    /// <summary>Servicios que la capa bajo prueba necesita además de la persistencia.</summary>
    protected virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    /// <summary>Claves de configuración extra que la capa bajo prueba necesita.</summary>
    protected virtual IEnumerable<KeyValuePair<string, string?>> AdditionalSettings => [];

    // Se usa el registro real de cada capa, no objetos armados a mano: así los tests también
    // cubren que las extensiones AddXxx enganchen todo lo que hace falta.
    private ServiceProvider BuildProvider(string connectionString)
    {
        var settings = new List<KeyValuePair<string, string?>>
        {
            new($"ConnectionStrings:{ServiceCollectionExtensions.ConnectionStringName}", connectionString)
        };

        settings.AddRange(AdditionalSettings);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddPersistence(configuration);

        ConfigureServices(services, configuration);

        return services.BuildServiceProvider();
    }

    private static string BuildConnectionString(string databaseName)
    {
        var server = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable) ?? DefaultServer;
        return $"{server};Database={databaseName};TrustServerCertificate=True";
    }

    private const string ConnectionEnvironmentVariable = "BAION_TEST_CONNECTION";

    private const string DefaultServer = @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True";
}
