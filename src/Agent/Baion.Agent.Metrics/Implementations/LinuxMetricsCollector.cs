using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Messages;
using Baion.Contracts.Metrics;
using Microsoft.Extensions.Logging;

namespace Baion.Agent.Metrics.Implementations;

/// <summary>
/// Lee las métricas de <c>/proc</c>, que es la fuente que ve el kernel y respeta los límites de cgroup
/// cuando el agente corre en un contenedor.
/// </summary>
internal class LinuxMetricsCollector(TimeProvider timeProvider, ILogger<LinuxMetricsCollector> logger) : IMetricsCollector
{
    private CpuSample? _previous;

    public async Task<MetricsReportMessage> CollectAsync(CancellationToken cancellationToken)
    {
        var cpu = await ReadCpuAsync(cancellationToken);
        var memory = await ReadMemoryAsync(cancellationToken);

        return new MetricsReportMessage(timeProvider.GetUtcNow(), cpu, memory, DiskMetricsReader.Read());
    }

    /// <summary>
    /// La primera línea de <c>/proc/stat</c> son contadores acumulados desde el arranque; el uso sale
    /// de la diferencia con la muestra anterior, no de un valor instantáneo.
    /// </summary>
    private async Task<CpuMetrics> ReadCpuAsync(CancellationToken cancellationToken)
    {
        var sample = await ReadCpuSampleAsync(cancellationToken);

        if (sample is null)
        {
            return new CpuMetrics(0, Environment.ProcessorCount, await ReadLoadAverageAsync(cancellationToken));
        }

        if (_previous is null)
        {
            // Sin referencia previa se toma una ventana corta, para no reportar un cero engañoso.
            await Task.Delay(FirstSampleWindow, timeProvider, cancellationToken);
            var segunda = await ReadCpuSampleAsync(cancellationToken) ?? sample;
            _previous = segunda;

            return new CpuMetrics(CpuSample.UsagePercent(sample, segunda), Environment.ProcessorCount, await ReadLoadAverageAsync(cancellationToken));
        }

        var usage = CpuSample.UsagePercent(_previous, sample);
        _previous = sample;

        return new CpuMetrics(usage, Environment.ProcessorCount, await ReadLoadAverageAsync(cancellationToken));
    }

    private async Task<CpuSample?> ReadCpuSampleAsync(CancellationToken cancellationToken)
    {
        var linea = await ReadFirstLineAsync(ProcStatPath, cancellationToken);

        if (linea is null || !linea.StartsWith("cpu ", StringComparison.Ordinal))
        {
            logger.LogDebug("No se pudo leer la línea agregada de {Ruta}", ProcStatPath);
            return null;
        }

        var campos = linea.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(ParseUlong).ToArray();

        if (campos.Length < 5)
        {
            return null;
        }

        // user, nice, system, idle, iowait, irq, softirq, steal...
        var total = campos.Aggregate(0UL, (acumulado, valor) => acumulado + valor);
        var idle = campos[3] + campos[4];

        return new CpuSample(total, idle);
    }

    private async Task<MemoryMetrics> ReadMemoryAsync(CancellationToken cancellationToken)
    {
        long total = 0;
        long available = 0;

        try
        {
            foreach (var linea in await File.ReadAllLinesAsync(ProcMemInfoPath, cancellationToken))
            {
                if (linea.StartsWith("MemTotal:", StringComparison.Ordinal))
                {
                    total = ParseKilobytes(linea);
                }
                else if (linea.StartsWith("MemAvailable:", StringComparison.Ordinal))
                {
                    available = ParseKilobytes(linea);
                    break;
                }
            }
        }
        catch (IOException exception)
        {
            logger.LogWarning("No se pudo leer {Ruta}: {Motivo}", ProcMemInfoPath, exception.Message);
        }

        return new MemoryMetrics(total, available);
    }

    private async Task<double?> ReadLoadAverageAsync(CancellationToken cancellationToken)
    {
        var linea = await ReadFirstLineAsync(ProcLoadAvgPath, cancellationToken);
        var primero = linea?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        return double.TryParse(primero, NumberStyles.Float, CultureInfo.InvariantCulture, out var carga) ? carga : null;
    }

    private async Task<string?> ReadFirstLineAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(path);
            return await reader.ReadLineAsync(cancellationToken);
        }
        catch (IOException exception)
        {
            logger.LogWarning("No se pudo leer {Ruta}: {Motivo}", path, exception.Message);
            return null;
        }
    }

    private static ulong ParseUlong(string value) => ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var resultado) ? resultado : 0;

    /// <summary>Las líneas de <c>/proc/meminfo</c> vienen como <c>Nombre:   12345 kB</c>.</summary>
    private static long ParseKilobytes(string line)
    {
        var partes = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length >= 2 && long.TryParse(partes[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var kilobytes) ? kilobytes * 1024 : 0;
    }

    private static readonly TimeSpan FirstSampleWindow = TimeSpan.FromMilliseconds(250);

    private const string ProcStatPath = "/proc/stat";

    private const string ProcMemInfoPath = "/proc/meminfo";

    private const string ProcLoadAvgPath = "/proc/loadavg";
}
