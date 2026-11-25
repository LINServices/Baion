using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;

// Construye el contexto para dotnet ef, que no arranca el host ni resuelve un tenant.
// La cadena de conexión se toma de BAION_DESIGN_CONNECTION y cae a LocalDB si no está.
internal class BaionDbContextFactory : IDesignTimeDbContextFactory<BaionDbContext>
{
    private const string ConnectionStringEnvironmentVariable = "BAION_DESIGN_CONNECTION";

    private const string DefaultConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=lin_baion;Trusted_Connection=True;TrustServerCertificate=True";

    public BaionDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable) ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<BaionDbContext>()
            .UseSqlServer(connectionString, sqlServer => sqlServer.MigrationsHistoryTable(BaionDbContext.MigrationsHistoryTable))
            .Options;

        return new BaionDbContext(options, new DesignTimeTenantContext());
    }
}
