namespace Baion.Orchestrator.Services;

/// <summary>
/// Parámetros de las consultas puntuales al agente por el socket: peticiones correlacionadas por
/// <c>RequestId</c> que esperan una respuesta en el momento, sin tocar la base.
/// </summary>
public class AgentQueryOptions
{
    /// <summary>Plazo para que el agente conteste una petición antes de darla por fallida.</summary>
    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>Líneas de log que se piden cuando el cliente no indica un número.</summary>
    public int DefaultLogLines { get; set; } = 200;

    /// <summary>
    /// Tope de líneas por instantánea de logs. La trama del protocolo son 1 MB; más allá de este número el
    /// agente recorta y marca la respuesta como truncada.
    /// </summary>
    public int MaxLogLines { get; set; } = 2000;

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "Orchestrator:AgentQuery";
}
