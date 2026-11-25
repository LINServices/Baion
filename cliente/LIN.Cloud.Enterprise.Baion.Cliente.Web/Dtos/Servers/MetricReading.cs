using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Servers;

public record MetricReading(DateTimeOffset CapturedAt, double CpuUsagePercent, int CpuCoreCount, double? LoadAverage1m, long MemoryTotalBytes, long MemoryAvailableBytes, IReadOnlyList<MetricDiskReading> Disks)
{
    public long MemoryUsedBytes => MemoryTotalBytes - MemoryAvailableBytes;

    public double MemoryUsagePercent => MemoryTotalBytes <= 0 ? 0 : MemoryUsedBytes * 100d / MemoryTotalBytes;
}
