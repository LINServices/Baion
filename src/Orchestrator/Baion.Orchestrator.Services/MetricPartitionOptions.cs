namespace Baion.Orchestrator.Services;

/// <summary>Mantenimiento de las particiones mensuales de la tabla de métricas.</summary>
public class MetricPartitionOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Meses hacia atrás para los que se asegura un límite de partición.</summary>
    public int PastMonths { get; set; } = 3;

    /// <summary>Meses de adelanto. Debe cubrir de sobra el intervalo entre comprobaciones.</summary>
    public int FutureMonths { get; set; } = 3;

    public int CheckIntervalHours { get; set; } = 24;

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "Orchestrator:MetricPartitions";
}
