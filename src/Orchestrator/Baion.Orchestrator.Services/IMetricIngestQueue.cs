using Baion.Orchestrator.Models.Dtos;

namespace Baion.Orchestrator.Services;

/// <summary>
/// Buzón entre el socket y la base de datos. Existe para que persistir una muestra nunca frene la
/// lectura del socket: el hilo que recibe encola y sigue leyendo.
/// </summary>
public interface IMetricIngestQueue
{
    /// <summary>Encola una muestra sin bloquear. Devuelve false si el buzón está lleno y hubo que descartarla.</summary>
    bool TryEnqueue(MetricSample sample);

    /// <summary>Muestras pendientes de escribir.</summary>
    int PendingCount { get; }
}
