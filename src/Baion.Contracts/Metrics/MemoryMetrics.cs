using System.Text.Json.Serialization;

namespace Baion.Contracts.Metrics;

/// <summary>Uso de memoria RAM del servidor en el momento de la captura.</summary>
public record MemoryMetrics(long TotalBytes, long AvailableBytes)
{
    [JsonIgnore]
    public long UsedBytes => TotalBytes - AvailableBytes;

    [JsonIgnore]
    public double UsagePercent => TotalBytes <= 0 ? 0 : UsedBytes * 100d / TotalBytes;
}
