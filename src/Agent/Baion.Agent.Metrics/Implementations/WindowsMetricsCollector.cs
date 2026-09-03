using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Messages;
using Baion.Contracts.Metrics;
using Microsoft.Extensions.Logging;

namespace Baion.Agent.Metrics.Implementations;

/// <summary>
/// Lee las métricas directamente de kernel32. Se evita a propósito <c>PerformanceCounter</c>: depende de
/// la infraestructura de contadores del sistema, que en servidores recortados o en contenedores no siempre
/// está disponible, y arrastra una dependencia solo por dos valores.
/// </summary>
[SupportedOSPlatform("windows")]
internal partial class WindowsMetricsCollector(TimeProvider timeProvider, ILogger<WindowsMetricsCollector> logger) : IMetricsCollector
{
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
        {
            return new CpuMetrics(0, Environment.ProcessorCount, null);
        }

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

    /// <summary>
    /// <c>GetSystemTimes</c> devuelve tiempos acumulados desde el arranque. El tiempo de kernel ya
    /// incluye el ocioso, así que el total es kernel + usuario.
    /// </summary>
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

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    private static readonly TimeSpan FirstSampleWindow = TimeSpan.FromMilliseconds(250);
}
