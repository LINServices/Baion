namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

// Consultas puntuales al agente por el socket: peticiones correlacionadas por RequestId que esperan
// respuesta en el momento, sin tocar la base.
public class AgentQueryOptions
{
    public const string SectionName = "Orchestrator:AgentQuery";

    // Plazo para que el agente conteste antes de darla por fallida.
    public int TimeoutSeconds { get; set; } = 20;

    // Líneas de log que se piden cuando el cliente no indica un número.
    public int DefaultLogLines { get; set; } = 200;

    // La trama del protocolo son 1 MB; más allá de este tope el agente recorta y marca la respuesta como truncada.
    public int MaxLogLines { get; set; } = 2000;
}
