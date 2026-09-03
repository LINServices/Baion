namespace Baion.Orchestrator.Models.Enums;

/// <summary>Estado agregado de un recorrido de cadena, deducido de las ejecuciones de sus pasos.</summary>
public enum ChainRunStatus
{
    /// <summary>Quedan pasos por ejecutar o hay uno en curso.</summary>
    Running = 1,

    /// <summary>Todos los pasos terminaron bien.</summary>
    Succeeded = 2,

    /// <summary>La cadena llegó al final, pero algún paso falló y su política era continuar.</summary>
    CompletedWithFailures = 3,

    /// <summary>Un paso falló con política de parada y la cadena no siguió.</summary>
    Stopped = 4
}
