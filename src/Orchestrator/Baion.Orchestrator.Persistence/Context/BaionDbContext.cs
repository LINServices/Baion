using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Baion.Orchestrator.Persistence.Context;

/// <summary>Contexto de la base <c>lin_baion</c>. Aplica el aislamiento por tenant a todo el modelo.</summary>
public class BaionDbContext(DbContextOptions<BaionDbContext> options, ITenantContext tenantContext) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Server> Servers => Set<Server>();

    public DbSet<ServerGroup> ServerGroups => Set<ServerGroup>();

    public DbSet<ServerGroupMember> ServerGroupMembers => Set<ServerGroupMember>();

    public DbSet<Script> Scripts => Set<Script>();

    public DbSet<ScriptChain> ScriptChains => Set<ScriptChain>();

    public DbSet<ScriptChainStep> ScriptChainSteps => Set<ScriptChainStep>();

    public DbSet<ScriptExecution> ScriptExecutions => Set<ScriptExecution>();

    public DbSet<ScheduledTask> ScheduledTasks => Set<ScheduledTask>();

    public DbSet<Metric> Metrics => Set<Metric>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<EnrollmentToken> EnrollmentTokens => Set<EnrollmentToken>();

    /// <summary>
    /// Tenant que parametriza el filtro global. Se lee en cada consulta, no al construir el modelo,
    /// por lo que un modelo compilado sirve a todos los tenants.
    /// </summary>
    public Guid? CurrentTenantId => tenantContext.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BaionDbContext).Assembly);
        ApplyTenantFilters(modelBuilder);
        ApplySnakeCaseNames(modelBuilder);
    }

    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        var tenantOwnedTypes = modelBuilder.Model
            .GetEntityTypes()
            .Where(entityType => !entityType.IsOwned() && typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
            .Select(entityType => entityType.ClrType)
            .Distinct()
            .ToList();

        foreach (var clrType in tenantOwnedTypes)
        {
            TenantFilterMethod.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);
        }
    }

    // Invocado por reflexión desde ApplyTenantFilters.
    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantOwned => modelBuilder.Entity<TEntity>().HasQueryFilter(entity => entity.TenantId == CurrentTenantId);

    /// <summary>Historial de migraciones, nombrado con la misma convención que el resto del esquema.</summary>
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    private static readonly MethodInfo TenantFilterMethod = typeof(BaionDbContext).GetMethod(nameof(ApplyTenantFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static void ApplySnakeCaseNames(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsMappedToJson())
            {
                continue;
            }

            var tableName = entityType.GetTableName();
            if (tableName is not null)
            {
                entityType.SetTableName(ToSnakeCase(tableName));
            }

            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }

            foreach (var key in entityType.GetKeys().Where(key => key.GetName() is not null))
            {
                key.SetName(ToSnakeCase(key.GetName()!));
            }

            foreach (var foreignKey in entityType.GetForeignKeys().Where(foreignKey => foreignKey.GetConstraintName() is not null))
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()!));
            }

            foreach (var index in entityType.GetIndexes().Where(index => index.GetDatabaseName() is not null))
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
            }
        }
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];
            var previousIsLower = i > 0 && char.IsLower(name[i - 1]);
            var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
            var needsSeparator = char.IsUpper(current) && i > 0 && name[i - 1] != '_' && (previousIsLower || nextIsLower);

            if (needsSeparator)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }
}
