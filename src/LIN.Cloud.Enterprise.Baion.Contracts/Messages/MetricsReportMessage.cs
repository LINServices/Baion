using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Metrics;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

/// <summary>Reporte periódico de CPU, RAM y disco del servidor donde corre el agente.</summary>
public record MetricsReportMessage(DateTimeOffset CapturedAt, CpuMetrics Cpu, MemoryMetrics Memory, IReadOnlyList<DiskMetrics> Disks) : AgentToServerMessage
{
    public const string TypeDiscriminator = "metrics-report";
}
