using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Contracts.Messages;
using LIN.Cloud.Enterprise.Baion.Contracts.Metrics;
using Microsoft.Extensions.Logging;

namespace LIN.Cloud.Enterprise.Baion.Agent.Metrics.Implementations;

// Lee las métricas directamente de kernel32. Se evita a propósito PerformanceCounter: depende de
// la infraestructura de contadores del sistema, que en servidores recortados o en contenedores no siempre
// está disponible, y arrastra una dependencia solo por dos valores.
[SupportedOSPlatform("windows")]
internal partial class WindowsMetricsCollector(TimeProvider timeProvider, ILogger<WindowsMetricsCollector> logger) : IMetricsCollector
{
    private static readonly TimeSpan FirstSampleWindow = TimeSpan.FromMilliseconds(250);

    private CpuSample? _previous;

    public async Task<MetricsReportMessage> CollectAsync(CancellationToken cancellationToken)
    {
        var cpu = await ReadCpuAsync(cancellationToken);

        return new MetricsReportMessage(timeProvider.GetUtcNow(), cpu, ReadMemory(), DiskMetricsReader.Read());
    }

    private async Task<CpuMetrics> ReadCpuAsync(CancellationToken cancellationToken)
    {
        var sample = ReadCpuSample();

        if (sample is null)
            return new CpuMetrics(0, Environment.ProcessorCount, null);

        if (_previous is null)
        {
            // Sin referencia previa se toma una ventana corta, para no reportar un cero engañoso.
            await Task.Delay(FirstSampleWindow, timeProvider, cancellationToken);
            var segunda = ReadCpuSample() ?? sample;
            _previous = segunda;

            return new CpuMetrics(CpuSample.UsagePercent(sample, segunda), Environment.ProcessorCount, null);
        }

        var usage = CpuSample.UsagePercent(_previous, sample);
        _previous = sample;

        return new CpuMetrics(usage, Environment.ProcessorCount, null);
    }

    // GetSystemTimes devuelve tiempos acumulados desde el arranque. El tiempo de kernel ya
    // incluye el ocioso, así que el total es kernel + usuario.
    private CpuSample? ReadCpuSample()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
        {
            logger.LogWarning("GetSystemTimes falló con el código {CodigoError}", Marshal.GetLastPInvokeError());
            return null;
        }

        return new CpuSample(kernel + user, idle);
    }

    private MemoryMetrics ReadMemory()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };

        if (!GlobalMemoryStatusEx(ref status))
        {
            logger.LogWarning("GlobalMemoryStatusEx falló con el código {CodigoError}", Marshal.GetLastPInvokeError());
            return new MemoryMetrics(0, 0);
        }

        return new MemoryMetrics((long)status.TotalPhysical, (long)status.AvailablePhysical);
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetSystemTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}
