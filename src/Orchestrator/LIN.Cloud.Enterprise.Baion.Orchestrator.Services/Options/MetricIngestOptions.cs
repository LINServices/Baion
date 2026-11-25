namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

public class MetricIngestOptions
{
    public const string SectionName = "Orchestrator:MetricIngest";

    // Al llenarse se descartan las nuevas: ante una sobrecarga es preferible perder telemetría que frenar
    // los sockets o quedarse sin memoria.
    public int QueueCapacity { get; set; } = 20_000;

    public int BatchSize { get; set; } = 200;

    public int BatchWindowMilliseconds { get; set; } = 1000;
}
