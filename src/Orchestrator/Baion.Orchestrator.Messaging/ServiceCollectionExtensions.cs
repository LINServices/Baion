using Baion.Orchestrator.Messaging.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baion.Orchestrator.Messaging;

/// <summary>Registro de dependencias de la capa.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registra el publicador y los consumidores de RabbitMQ.</summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection(RabbitMqOptions.SectionName));

        var settings = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        // Sin broker el orquestador sigue siendo funcional, solo que limitado a sus propios agentes.
        if (!settings.Enabled)
        {
            services.AddSingleton<IAgentCommandBus, LocalAgentCommandBus>();
            services.AddSingleton<IAgentPresenceBus, NoOpAgentPresenceBus>();
            services.AddSingleton<IAgentCommandSubscription, NoOpAgentCommandSubscription>();

            return services;
        }

        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddHostedService(provider => provider.GetRequiredService<RabbitMqConnectionProvider>());
        services.AddSingleton<IAgentCommandBus, RabbitMqAgentCommandBus>();
        services.AddSingleton<IAgentPresenceBus, RabbitMqAgentPresenceBus>();

        services.AddSingleton<RabbitMqAgentCommandSubscription>();
        services.AddSingleton<IAgentCommandSubscription>(provider => provider.GetRequiredService<RabbitMqAgentCommandSubscription>());
        services.AddHostedService(provider => provider.GetRequiredService<RabbitMqAgentCommandSubscription>());

        services.AddHostedService<AgentPresenceConsumerHostedService>();

        return services;
    }
}
