using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Baion.Orchestrator.Messaging;
using Baion.Orchestrator.Persistence;
using Baion.Orchestrator.Persistence.Context;
using Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

/// <summary>
/// Levanta el orquestador completo — el mismo <c>Program</c> de producción — contra una base
/// <c>lin_baion</c> desechable, para que el handshake se pruebe sobre un WebSocket de verdad.
/// </summary>
public class OrchestratorFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly bool _ownsDatabase;

    private readonly bool _enableRabbitMq;

    /// <summary>xUnit activa los fixtures de clase por constructor sin parámetros, y solo admite uno público.</summary>
    public OrchestratorFactory()
    {
        DatabaseName = $"lin_baion_agents_{Guid.NewGuid():N}";
        _ownsDatabase = true;
    }

    /// <summary>Instancia adicional sobre una base ya creada por otra fábrica.</summary>
    internal OrchestratorFactory(string? databaseName, bool enableRabbitMq)
    {
        DatabaseName = databaseName ?? $"lin_baion_agents_{Guid.NewGuid():N}";
        _ownsDatabase = databaseName is null;
        _enableRabbitMq = enableRabbitMq;
    }

    /// <summary>Base que usa esta instancia. Pasarla a otra fábrica levanta una segunda sobre la misma.</summary>
    public string DatabaseName { get; }

    /// <summary>Instancia declarada por esta aplicación, para poder simular su reinicio en los tests.</summary>
    public string InstanceId { get; } = $"tests-{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        if (!_ownsDatabase)
        {
            // Arranca la aplicación sin tocar el esquema: la creó y migró la primera instancia.
            _ = Services;
            return;
        }

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        // Primero se para el host: sus servicios en segundo plano siguen consultando la base, y borrarla
        // con ellos vivos hace fallar tanto al borrado como a los propios servicios.
        await base.DisposeAsync();

        if (_ownsDatabase)
        {
            await DropDatabaseAsync();
        }
    }

    /// <summary>Borra la base con una conexión propia, ya sin la aplicación levantada.</summary>
    private async Task DropDatabaseAsync()
    {
        var maestra = new SqlConnectionStringBuilder(BuildConnectionString(DatabaseName)) { InitialCatalog = "master" }.ConnectionString;

        await using var connection = new SqlConnection(maestra);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        // SINGLE_USER expulsa cualquier conexión que hubiera quedado colgando del pool.
        command.CommandText = $"""
            IF DB_ID('{DatabaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{DatabaseName}];
            END
            """;

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Abre un scope ya posicionado en un tenant, como haría el middleware de la API.</summary>
    public AsyncServiceScope CreateTenantScope(Guid tenantId)
    {
        var scope = Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(tenantId);
        return scope;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);

        // Fuera de Development la validación no se activa sola, y es la que caza a un singleton
        // consumiendo algo con ámbito de petición.
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        foreach (var (clave, valor) in TestSettings)
        {
            builder.UseSetting(clave, valor);
        }
    }

    // UseSetting entra como configuración de host, que se resuelve antes de los appsettings del proyecto.
    private Dictionary<string, string?> TestSettings => new()
    {
        [$"ConnectionStrings:{Baion.Orchestrator.Persistence.ServiceCollectionExtensions.ConnectionStringName}"] = BuildConnectionString(DatabaseName),
        [$"{RabbitMqOptions.SectionName}:Enabled"] = _enableRabbitMq ? "true" : "false",
        ["Identity:Issuer"] = "baion-tests",
        ["Identity:Audience"] = "baion-tests",
        ["Identity:SigningKey"] = "clave-de-pruebas-suficientemente-larga-para-hmac-sha256",
        ["Identity:Bootstrap:Enabled"] = "false",
        [$"{OrchestratorOptions.SectionName}:InstanceId"] = InstanceId,
        [$"{OrchestratorOptions.SectionName}:HeartbeatSeconds"] = "1",
        [$"{OrchestratorOptions.SectionName}:HandshakeTimeoutSeconds"] = "5",
        [$"{SchedulerOptions.SectionName}:TickSeconds"] = "1",
        [$"{ScriptEventOptions.SectionName}:BatchWindowMilliseconds"] = "100"
    };

    private static string BuildConnectionString(string databaseName)
    {
        var server = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable) ?? DefaultServer;
        return $"{server};Database={databaseName};TrustServerCertificate=True";
    }

    private const string ConnectionEnvironmentVariable = "BAION_TEST_CONNECTION";

    private const string DefaultServer = @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True";
}
