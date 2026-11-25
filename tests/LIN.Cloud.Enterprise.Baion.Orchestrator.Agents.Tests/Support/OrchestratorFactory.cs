using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Messaging;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Support;

// Levanta el orquestador completo — el mismo Program de producción — contra una base
// lin_baion desechable, para que el handshake se pruebe sobre un WebSocket de verdad.
public class OrchestratorFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ConnectionEnvironmentVariable = "BAION_TEST_CONNECTION";

    private const string DefaultServer = @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True";

    private readonly bool _ownsDatabase;

    private readonly bool _enableRabbitMq;

    // xUnit activa los fixtures de clase por constructor sin parámetros, y solo admite uno público.
    public OrchestratorFactory()
    {
        DatabaseName = $"lin_baion_agents_{Guid.NewGuid():N}";
        _ownsDatabase = true;
    }

    // Instancia adicional sobre una base ya creada por otra fábrica.
    internal OrchestratorFactory(string? databaseName, bool enableRabbitMq)
    {
        DatabaseName = databaseName ?? $"lin_baion_agents_{Guid.NewGuid():N}";
        _ownsDatabase = databaseName is null;
        _enableRabbitMq = enableRabbitMq;
    }

    public string DatabaseName { get; }

    // Instancia declarada por esta aplicación, para poder simular su reinicio en los tests.
    public string InstanceId { get; } = $"tests-{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        if (!_ownsDatabase)
        {
            // Arranca la aplicación sin tocar el esquema: la creó y migró la primera instancia.
            _ = Services;
            return;
        }

        // El esquema se migra antes de arrancar el Program de producción: su EnsureCreatedAsync no hace nada
        // sobre una base con tablas, y así se conservan las particiones de metrics que solo crea la migración.
        await MigrateSchemaAsync();
        _ = Services;
    }

    private async Task MigrateSchemaAsync()
    {
        var options = new DbContextOptionsBuilder<BaionDbContext>()
            .UseSqlServer(BuildConnectionString(DatabaseName), sqlServer => sqlServer.MigrationsHistoryTable(BaionDbContext.MigrationsHistoryTable))
            .Options;

        await using var context = new BaionDbContext(options, new MigrationTenantContext());
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        // Primero se para el host: sus servicios en segundo plano siguen consultando la base, y borrarla
        // con ellos vivos hace fallar tanto al borrado como a los propios servicios.
        await base.DisposeAsync();

        if (_ownsDatabase)
            await DropDatabaseAsync();
    }

    // Borra la base con una conexión propia, ya sin la aplicación levantada.
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

    // Abre un scope ya posicionado en un tenant, como haría el middleware de la API.
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
            builder.UseSetting(clave, valor);

        // TestServer no rellena RemoteIpAddress y UseLINHttp responde 400 si no puede resolver la IP del cliente.
        builder.ConfigureTestServices(services => services.AddTransient<IStartupFilter, RemoteIpStartupFilter>());
    }

    // UseSetting entra como configuración de host, que se resuelve antes de los appsettings del proyecto.
    private Dictionary<string, string?> TestSettings => new()
    {
        [$"ConnectionStrings:{LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.ServiceCollectionExtensions.ConnectionStringName}"] = BuildConnectionString(DatabaseName),
        [$"{RabbitMqOptions.SectionName}:Enabled"] = _enableRabbitMq ? "true" : "false",
        ["Identity:Issuer"] = "baion-tests",
        ["Identity:Audience"] = "baion-tests",
        ["Identity:SigningKey"] = "clave-de-pruebas-suficientemente-larga-para-hmac-sha256",
        ["Identity:Bootstrap:Enabled"] = "false",
        [$"{OrchestratorOptions.SectionName}:InstanceId"] = InstanceId,
        [$"{OrchestratorOptions.SectionName}:HeartbeatSeconds"] = "1",
        [$"{OrchestratorOptions.SectionName}:HandshakeTimeoutSeconds"] = "5",
        [$"{SchedulerOptions.SectionName}:TickSeconds"] = "1",
        [$"{ScriptEventOptions.SectionName}:BatchWindowMilliseconds"] = "100",
        [$"{AgentQueryOptions.SectionName}:TimeoutSeconds"] = "3"
    };

    private static string BuildConnectionString(string databaseName)
    {
        var server = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable) ?? DefaultServer;
        return $"{server};Database={databaseName};TrustServerCertificate=True";
    }
}
