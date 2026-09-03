using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Baion.Orchestrator.Persistence.Context;

/// <summary>
/// Construye el contexto para <c>dotnet ef</c>, que no arranca el host ni resuelve un tenant.
/// La cadena de conexión se toma de <c>BAION_DESIGN_CONNECTION</c> y cae a LocalDB si no está.
/// </summary>
internal class BaionDbContextFactory : IDesignTimeDbContextFactory<BaionDbContext>
{
    public BaionDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable) ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<BaionDbContext>()
            .UseSqlServer(connectionString, sqlServer => sqlServer.MigrationsHistoryTable(BaionDbContext.MigrationsHistoryTable))
            .Options;

        return new BaionDbContext(options, new DesignTimeTenantContext());
    }

    private const string ConnectionStringEnvironmentVariable = "BAION_DESIGN_CONNECTION";

    private const string DefaultConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=lin_baion;Trusted_Connection=True;TrustServerCertificate=True";

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? TenantId => null;

        public void SetTenant(Guid tenantId) => throw new NotSupportedException("El contexto de diseño no opera sobre un tenant.");
    }
}
