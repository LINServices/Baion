using System.Text.Json.Serialization;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Metrics;

public record DiskMetrics(string Name, string MountPoint, long TotalBytes, long AvailableBytes)
{
    [JsonIgnore]
    public long UsedBytes => TotalBytes - AvailableBytes;

    [JsonIgnore]
    public double UsagePercent => TotalBytes <= 0 ? 0 : UsedBytes * 100d / TotalBytes;
}
