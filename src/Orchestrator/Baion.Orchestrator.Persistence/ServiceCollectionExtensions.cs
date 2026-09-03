using System;
using Baion.Orchestrator.Messaging;
using Baion.Orchestrator.Persistence.Context;
using Baion.Orchestrator.Persistence.Implementations;
using Baion.Orchestrator.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Baion.Orchestrator.Persistence;

/// <summary>Registro de dependencias de la capa.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registra el contexto de datos y los repositorios.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Falta la cadena de conexión '{ConnectionStringName}' en la configuración.");
        }

        services.AddOptions<PresenceOptions>().Bind(configuration.GetSection(PresenceOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<TenantStampInterceptor>();
        services.AddScoped<AuditTimestampsInterceptor>();

        services.AddDbContext<BaionDbContext>((provider, options) => options
            .UseSqlServer(connectionString, sqlServer => sqlServer.MigrationsHistoryTable(BaionDbContext.MigrationsHistoryTable))
            .AddInterceptors(provider.GetRequiredService<TenantStampInterceptor>(), provider.GetRequiredService<AuditTimestampsInterceptor>()));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IScriptExecutionRepository, ScriptExecutionRepository>();
        services.AddScoped<IScriptChainRepository, ScriptChainRepository>();
        services.AddScoped<IScheduledTaskRepository, ScheduledTaskRepository>();
        services.AddScoped<IAgentPresenceLookup, AgentPresenceLookup>();
        services.AddScoped<IServerQueries, ServerQueries>();
        services.AddScoped<IScriptQueries, ScriptQueries>();

        return services;
    }

    /// <summary>Clave de la cadena de conexión a <c>lin_baion</c> dentro de <c>ConnectionStrings</c>.</summary>
    public const string ConnectionStringName = "BaionDatabase";
}
