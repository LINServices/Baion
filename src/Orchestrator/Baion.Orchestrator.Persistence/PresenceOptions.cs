namespace Baion.Orchestrator.Persistence;

/// <summary>Vigencia del registro de presencia de los agentes.</summary>
public class PresenceOptions
{
    /// <summary>
    /// Antigüedad máxima de la última señal de vida para seguir dando por conectado a un servidor.
    /// Debe holgar varios latidos: con la cadencia por defecto de 30 s, 120 s tolera dos perdidos.
    /// </summary>
    public int TimeToLiveSeconds { get; set; } = 120;

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "Orchestrator:Presence";
}
