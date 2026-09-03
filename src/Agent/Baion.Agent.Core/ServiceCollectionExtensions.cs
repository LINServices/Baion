using Baion.Agent.Core.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baion.Agent.Core;

/// <summary>Registro de dependencias de la capa.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registra el cliente WebSocket y el coordinador de concurrencia del agente.</summary>
    public static IServiceCollection AddAgentCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AgentOptions>().Bind(configuration.GetSection(AgentOptions.SectionName));

        services.AddSingleton<IPlatformInfoProvider, PlatformInfoProvider>();
        services.AddSingleton<IAgentStateStore, FileAgentStateStore>();
        services.AddSingleton<IReconnectPolicy, ExponentialBackoffReconnectPolicy>();

        services.AddSingleton<OrchestratorChannel>();
        services.AddSingleton<IOrchestratorChannel>(provider => provider.GetRequiredService<OrchestratorChannel>());

        services.AddHostedService<AgentConnectionWorker>();

        return services;
    }
}
