namespace Baion.Orchestrator.Models.Entities;

/// <summary>Uso de un volumen dentro de una muestra de métricas. Tipo owned, serializado a JSON.</summary>
public class MetricDisk
{
    public string Name { get; set; } = string.Empty;

    public string MountPoint { get; set; } = string.Empty;

    public long TotalBytes { get; set; }

    public long AvailableBytes { get; set; }
}
