using System;
using Baion.Orchestrator.Messaging;
using Baion.Orchestrator.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Baion.Orchestrator.Services;

/// <summary>Registro de dependencias de la capa.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registra los servicios de aplicación del orquestador.</summary>
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OrchestratorOptions>().Bind(configuration.GetSection(OrchestratorOptions.SectionName));
        services.AddOptions<MetricIngestOptions>().Bind(configuration.GetSection(MetricIngestOptions.SectionName));
        services.AddOptions<MetricPartitionOptions>().Bind(configuration.GetSection(MetricPartitionOptions.SectionName));
        services.AddOptions<ScriptEventOptions>().Bind(configuration.GetSection(ScriptEventOptions.SectionName));
        services.AddOptions<SchedulerOptions>().Bind(configuration.GetSection(SchedulerOptions.SectionName));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IAgentRegistry, AgentRegistry>();
        services.AddSingleton<ILocalAgentDelivery, LocalAgentDelivery>();
        services.AddSingleton<IAgentConnectionHandler, AgentConnectionHandler>();
        services.AddSingleton<IMetricIngestQueue, MetricIngestQueue>();
        services.AddSingleton<IScriptEventQueue, ScriptEventQueue>();

        services.AddScoped<IAgentEnrollmentService, AgentEnrollmentService>();
        services.AddScoped<IEnrollmentTokenService, EnrollmentTokenService>();
        services.AddScoped<IScriptService, ScriptService>();
        services.AddScoped<IScriptDispatchService, ScriptDispatchService>();
        services.AddScoped<IScriptChainService, ScriptChainService>();
        services.AddScoped<IScheduledTaskService, ScheduledTaskService>();
        services.AddScoped<IServerService, ServerService>();

        services.AddHostedService<InstancePresenceHostedService>();
        services.AddHostedService<MetricIngestHostedService>();
        services.AddHostedService<MetricPartitionMaintenanceHostedService>();
        services.AddHostedService<ScriptEventIngestHostedService>();
        services.AddHostedService<SchedulerHostedService>();

        return services;
    }
}
