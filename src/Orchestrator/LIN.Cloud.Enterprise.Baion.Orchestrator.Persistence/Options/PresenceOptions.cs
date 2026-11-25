namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;

public class PresenceOptions
{
    public const string SectionName = "Orchestrator:Presence";

    // Antigüedad máxima de la última señal de vida para seguir dando por conectado a un servidor.
    // Debe holgar varios latidos: con la cadencia por defecto de 30 s, 120 s tolera dos perdidos.
    public int TimeToLiveSeconds { get; set; } = 120;
}
