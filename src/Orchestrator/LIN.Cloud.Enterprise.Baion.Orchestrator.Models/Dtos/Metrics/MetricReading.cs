using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record MetricReading(DateTimeOffset CapturedAt, double CpuUsagePercent, int CpuCoreCount, double? LoadAverage1m, long MemoryTotalBytes, long MemoryAvailableBytes, IReadOnlyList<MetricDiskReading> Disks);
