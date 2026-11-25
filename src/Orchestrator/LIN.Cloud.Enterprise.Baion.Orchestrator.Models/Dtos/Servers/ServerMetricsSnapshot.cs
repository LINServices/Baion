using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ServerMetricsSnapshot(DateTimeOffset CapturedAt, double CpuUsagePercent, int CpuCoreCount, long MemoryTotalBytes, long MemoryAvailableBytes);
