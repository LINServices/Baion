using LIN.Cloud.Enterprise.Baion.Agent.Core.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LIN.Cloud.Enterprise.Baion.Agent.Core;

public static class ServiceCollectionExtensions
{
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
