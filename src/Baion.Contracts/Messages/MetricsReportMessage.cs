using System;
using System.Collections.Generic;
using Baion.Contracts.Metrics;

namespace Baion.Contracts.Messages;

/// <summary>Reporte periódico de CPU, RAM y disco del servidor donde corre el agente.</summary>
public record MetricsReportMessage(DateTimeOffset CapturedAt, CpuMetrics Cpu, MemoryMetrics Memory, IReadOnlyList<DiskMetrics> Disks) : AgentToServerMessage
{
    public const string TypeDiscriminator = "metrics-report";
}
