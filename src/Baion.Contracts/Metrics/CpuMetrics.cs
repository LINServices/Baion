namespace Baion.Contracts.Metrics;

/// <summary>Uso de CPU del servidor en el momento de la captura.</summary>
public record CpuMetrics(double UsagePercent, int CoreCount, double? LoadAverage1m);
