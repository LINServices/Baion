using System.Collections.Generic;
using System.IO;
using System.Linq;
using Baion.Contracts.Metrics;

namespace Baion.Agent.Metrics.Implementations;

/// <summary>
/// Lee los volúmenes fijos con <c>DriveInfo</c>, que funciona igual en Linux y en Windows.
/// Es la única parte de las métricas que no necesita código específico de plataforma.
/// </summary>
internal static class DiskMetricsReader
{
    public static IReadOnlyList<DiskMetrics> Read() => DriveInfo.GetDrives()
        .Where(drive => drive.IsReady && drive.DriveType is DriveType.Fixed && drive.TotalSize > 0)
        .Select(drive => new DiskMetrics(drive.Name, drive.RootDirectory.FullName, drive.TotalSize, drive.AvailableFreeSpace))
        .ToList();
}
