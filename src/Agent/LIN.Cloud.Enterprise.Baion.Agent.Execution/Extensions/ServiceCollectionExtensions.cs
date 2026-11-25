using System;
using LIN.Cloud.Enterprise.Baion.Agent.Core;
using LIN.Cloud.Enterprise.Baion.Agent.Execution.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LIN.Cloud.Enterprise.Baion.Agent.Execution;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScriptExecution(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ScriptExecutionOptions>().Bind(configuration.GetSection(ScriptExecutionOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);

        if (OperatingSystem.IsWindows())
            services.AddSingleton<IScriptExecutor, WindowsScriptExecutor>();
        else
            services.AddSingleton<IScriptExecutor, LinuxScriptExecutor>();

        // El coordinador también es hosted service: al parar el agente espera a que lo que corre informe su final.
        services.AddSingleton<ScriptExecutionCoordinator>();
        services.AddSingleton<IScriptExecutionCoordinator>(provider => provider.GetRequiredService<ScriptExecutionCoordinator>());
        services.AddHostedService(provider => provider.GetRequiredService<ScriptExecutionCoordinator>());

        services.AddSingleton<IServerMessageHandler, ExecuteScriptHandler>();

        return services;
    }
}
