using System;
using System.Collections.Generic;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Muestra puntual de CPU, RAM y disco de un servidor. Tabla de alto volumen, particionada por fecha.</summary>
public class Metric : ITenantOwned
{
    public long Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid ServerId { get; set; }

    public Server Server { get; set; } = null!;

    public DateTimeOffset CapturedAt { get; set; }

    public double CpuUsagePercent { get; set; }

    public int CpuCoreCount { get; set; }

    public double? LoadAverage1m { get; set; }

    public long MemoryTotalBytes { get; set; }

    public long MemoryAvailableBytes { get; set; }

    /// <summary>Volúmenes del servidor, guardados como JSON dentro de la misma fila para no partir la tabla.</summary>
    public ICollection<MetricDisk> Disks { get; set; } = [];
}
