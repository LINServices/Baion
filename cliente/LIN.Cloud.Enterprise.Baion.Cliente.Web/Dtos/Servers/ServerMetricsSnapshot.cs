using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Servers;

public record ServerMetricsSnapshot(DateTimeOffset CapturedAt, double CpuUsagePercent, int CpuCoreCount, long MemoryTotalBytes, long MemoryAvailableBytes)
{
    public long MemoryUsedBytes => MemoryTotalBytes - MemoryAvailableBytes;

    public double MemoryUsagePercent => MemoryTotalBytes <= 0 ? 0 : MemoryUsedBytes * 100d / MemoryTotalBytes;
}
