namespace Baion.Agent.Metrics;

/// <summary>Configuración del reporte de métricas.</summary>
public class MetricsOptions
{
    /// <summary>Cadencia con la que el agente toma y envía una muestra.</summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "Metrics";
}
