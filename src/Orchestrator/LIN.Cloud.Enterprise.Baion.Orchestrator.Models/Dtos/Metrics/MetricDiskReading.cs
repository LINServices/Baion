namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record MetricDiskReading(string Name, string MountPoint, long TotalBytes, long AvailableBytes);
