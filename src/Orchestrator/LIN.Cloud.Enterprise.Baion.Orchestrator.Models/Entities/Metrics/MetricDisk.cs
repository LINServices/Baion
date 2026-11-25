namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

// Uso de un volumen dentro de una muestra de métricas. Tipo owned, serializado a JSON.
public class MetricDisk
{
    public string Name { get; set; } = string.Empty;

    public string MountPoint { get; set; } = string.Empty;

    public long TotalBytes { get; set; }

    public long AvailableBytes { get; set; }
}
