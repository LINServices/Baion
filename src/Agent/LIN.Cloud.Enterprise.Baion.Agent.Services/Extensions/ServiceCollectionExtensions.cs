using System;
using LIN.Cloud.Enterprise.Baion.Agent.Core;
using LIN.Cloud.Enterprise.Baion.Agent.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LIN.Cloud.Enterprise.Baion.Agent.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceInspection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ServiceInspectionOptions>().Bind(configuration.GetSection(ServiceInspectionOptions.SectionName));

        if (OperatingSystem.IsWindows())
            services.AddSingleton<IServiceInspector, WindowsServiceInspector>();
        else
            services.AddSingleton<IServiceInspector, LinuxServiceInspector>();

        // El procesador también es hosted service: al parar el agente espera a que las consultas en curso respondan.
        services.AddSingleton<ServiceQueryProcessor>();
        services.AddSingleton<IServiceQueryProcessor>(provider => provider.GetRequiredService<ServiceQueryProcessor>());
        services.AddHostedService(provider => provider.GetRequiredService<ServiceQueryProcessor>());

        services.AddSingleton<IServerMessageHandler, ServiceQueryHandler>();

        return services;
    }
}
