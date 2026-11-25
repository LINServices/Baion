using System.Collections.Generic;
using System.Linq;
using System.Text;
using LIN.Cloud.Enterprise.Baion.Contracts.Services;

namespace LIN.Cloud.Enterprise.Baion.Agent.Services.Implementations;

// Recorta una instantánea de logs para que quepa en la trama del protocolo. Se descartan las líneas más
// antiguas: ante un tope, lo reciente es lo que importa.
internal static class LogBudget
{
    public static (IReadOnlyList<ServiceLogLine> Lines, bool Truncated) Fit(IReadOnlyList<ServiceLogLine> lines, int maxBytes, bool alreadyTruncated)
    {
        var total = lines.Sum(EstimateBytes);

        if (total <= maxBytes)
            return (lines, alreadyTruncated);

        var kept = new LinkedList<ServiceLogLine>();
        var used = 0L;

        var index = lines.Count - 1;

        // Se conservan las más recientes; la última entra siempre aunque ella sola exceda el tope.
        while (index >= 0 && (kept.Count == 0 || used + EstimateBytes(lines[index]) <= maxBytes))
        {
            kept.AddFirst(lines[index]);
            used += EstimateBytes(lines[index]);
            index--;
        }

        return (kept.ToList(), true);
    }

    // Tamaño aproximado de la línea ya serializada: mensaje + nivel + el andamiaje JSON y la marca de tiempo.
    private static int EstimateBytes(ServiceLogLine line)
    {
        return Encoding.UTF8.GetByteCount(line.Message) + (line.Level?.Length ?? 0) + 48;
    }
}
