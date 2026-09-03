namespace Baion.Orchestrator.Services;

/// <summary>Parámetros de la ingesta de métricas.</summary>
public class MetricIngestOptions
{
    /// <summary>
    /// Tope de muestras en espera. Al llenarse se descartan las nuevas: ante una sobrecarga es preferible
    /// perder telemetría que frenar los sockets o quedarse sin memoria.
    /// </summary>
    public int QueueCapacity { get; set; } = 20_000;

    /// <summary>Muestras por lote de escritura.</summary>
    public int BatchSize { get; set; } = 200;

    /// <summary>Espera máxima antes de escribir un lote incompleto.</summary>
    public int BatchWindowMilliseconds { get; set; } = 1000;

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "Orchestrator:MetricIngest";
}
