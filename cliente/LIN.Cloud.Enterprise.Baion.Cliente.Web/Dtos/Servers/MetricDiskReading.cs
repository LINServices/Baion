namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Servers;

public record MetricDiskReading(string Name, string MountPoint, long TotalBytes, long AvailableBytes)
{
    public long UsedBytes => TotalBytes - AvailableBytes;

    public double UsagePercent => TotalBytes <= 0 ? 0 : UsedBytes * 100d / TotalBytes;
}
