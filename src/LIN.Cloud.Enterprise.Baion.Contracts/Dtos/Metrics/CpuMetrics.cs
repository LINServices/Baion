namespace LIN.Cloud.Enterprise.Baion.Contracts.Metrics;

public record CpuMetrics(double UsagePercent, int CoreCount, double? LoadAverage1m);
