namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

public class MetricPartitionOptions
{
    public const string SectionName = "Orchestrator:MetricPartitions";

    public bool Enabled { get; set; } = true;

    // Meses hacia atrás para los que se asegura un límite de partición.
    public int PastMonths { get; set; } = 3;

    // Debe cubrir de sobra el intervalo entre comprobaciones.
    public int FutureMonths { get; set; } = 3;

    public int CheckIntervalHours { get; set; } = 24;
}
