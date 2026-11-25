using System.Collections.Generic;
using System.IO;
using System.Linq;
using LIN.Cloud.Enterprise.Baion.Contracts.Metrics;

namespace LIN.Cloud.Enterprise.Baion.Agent.Metrics.Implementations;

// Lee los volúmenes fijos con DriveInfo, que funciona igual en Linux y en Windows.
// Es la única parte de las métricas que no necesita código específico de plataforma.
internal static class DiskMetricsReader
{
    public static IReadOnlyList<DiskMetrics> Read()
    {
        return DriveInfo.GetDrives()
            .Where(drive => drive.IsReady && drive.DriveType is DriveType.Fixed && drive.TotalSize > 0)
            .Select(drive => new DiskMetrics(drive.Name, drive.RootDirectory.FullName, drive.TotalSize, drive.AvailableFreeSpace))
            .ToList();
    }
}
