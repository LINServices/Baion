using System;
using Baion.Agent.Metrics.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Baion.Agent.Metrics;

/// <summary>Registro de dependencias de la capa.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registra el recolector de métricas correspondiente a la plataforma detectada.</summary>
    public static IServiceCollection AddMetricsCollection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MetricsOptions>().Bind(configuration.GetSection(MetricsOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);

        // El recolector guarda la muestra de CPU anterior para calcular el uso, así que es singleton.
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<IMetricsCollector, WindowsMetricsCollector>();
        }
        else
        {
            services.AddSingleton<IMetricsCollector, LinuxMetricsCollector>();
        }

        services.AddHostedService<MetricsReportingWorker>();

        return services;
    }
}
