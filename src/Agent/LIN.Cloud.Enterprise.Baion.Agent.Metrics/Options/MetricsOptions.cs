namespace LIN.Cloud.Enterprise.Baion.Agent.Metrics;

public class MetricsOptions
{
    public const string SectionName = "Metrics";

    // Cadencia con la que el agente toma y envía una muestra.
    public int IntervalSeconds { get; set; } = 30;
}
