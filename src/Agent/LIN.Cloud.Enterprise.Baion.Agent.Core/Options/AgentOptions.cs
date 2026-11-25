namespace LIN.Cloud.Enterprise.Baion.Agent.Core;

public class AgentOptions
{
    public const string SectionName = "Agent";

    // Base del orquestador, con esquema ws:// o wss://.
    public string OrchestratorUri { get; set; } = "ws://localhost:5199";

    // Solo hace falta hasta que el agente obtiene su credencial permanente.
    public string? EnrollmentToken { get; set; }

    // Vacío significa la ruta estándar de la plataforma.
    public string StateDirectory { get; set; } = string.Empty;

    public int MinReconnectSeconds { get; set; } = 1;

    public int MaxReconnectSeconds { get; set; } = 60;
}
