using System.Collections.Generic;
using System.Linq;
using System.Text;
using Baion.Contracts.Services;

namespace Baion.Agent.Services.Implementations;

/// <summary>
/// Recorta una instantánea de logs para que quepa en la trama del protocolo. Se descartan las líneas más
/// antiguas: ante un tope, lo reciente es lo que importa.
/// </summary>
internal static class LogBudget
{
    public static (IReadOnlyList<ServiceLogLine> Lines, bool Truncated) Fit(IReadOnlyList<ServiceLogLine> lines, int maxBytes, bool alreadyTruncated)
    {
        var total = lines.Sum(EstimateBytes);

        if (total <= maxBytes)
        {
            return (lines, alreadyTruncated);
        }

        var kept = new LinkedList<ServiceLogLine>();
        var used = 0L;

        for (var index = lines.Count - 1; index >= 0; index--)
        {
            var size = EstimateBytes(lines[index]);

            if (kept.Count > 0 && used + size > maxBytes)
            {
                break;
            }

            kept.AddFirst(lines[index]);
            used += size;
        }

        return (kept.ToList(), true);
    }

    /// <summary>Tamaño aproximado de la línea ya serializada: mensaje + nivel + el andamiaje JSON y la marca de tiempo.</summary>
    private static int EstimateBytes(ServiceLogLine line) => Encoding.UTF8.GetByteCount(line.Message) + (line.Level?.Length ?? 0) + 48;
}
