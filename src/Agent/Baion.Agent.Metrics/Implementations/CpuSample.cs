using System;

namespace Baion.Agent.Metrics.Implementations;

/// <summary>
/// Contadores acumulados de CPU en un instante. Los dos sistemas operativos exponen tiempo total y
/// tiempo ocioso desde el arranque, así que el uso siempre sale de la diferencia entre dos muestras.
/// </summary>
internal record CpuSample(ulong TotalTicks, ulong IdleTicks)
{
    public static double UsagePercent(CpuSample previous, CpuSample current)
    {
        var total = current.TotalTicks - previous.TotalTicks;
        var idle = current.IdleTicks - previous.IdleTicks;

        // Un contador que retrocede solo pasa si el sistema reinició sus estadísticas: se descarta la ventana.
        if (current.TotalTicks < previous.TotalTicks || total == 0)
        {
            return 0;
        }

        var usage = (total - Math.Min(idle, total)) * 100d / total;

        return Math.Round(Math.Clamp(usage, 0, 100), 2);
    }
}
