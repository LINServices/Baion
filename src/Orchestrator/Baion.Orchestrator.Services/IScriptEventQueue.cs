using Baion.Orchestrator.Models.Dtos;

namespace Baion.Orchestrator.Services;

/// <summary>
/// Buzón entre el socket y la base para las novedades de ejecución. Igual que con las métricas, el hilo
/// que recibe encola y sigue leyendo; aquí además el orden importa, y un único lector lo garantiza.
/// </summary>
public interface IScriptEventQueue
{
    /// <summary>Encola una novedad sin bloquear. Devuelve false si el buzón está lleno.</summary>
    bool TryEnqueue(ScriptExecutionEvent notification);

    /// <summary>Novedades pendientes de escribir.</summary>
    int PendingCount { get; }
}
