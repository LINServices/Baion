namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

// Estado agregado de un recorrido de cadena, deducido de las ejecuciones de sus pasos.
public enum ChainRunStatus
{
    // Quedan pasos por ejecutar o hay uno en curso.
    Running = 1,

    // Todos los pasos terminaron bien.
    Succeeded = 2,

    // La cadena llegó al final, pero algún paso falló y su política era continuar.
    CompletedWithFailures = 3,

    // Un paso falló con política de parada y la cadena no siguió.
    Stopped = 4
}
