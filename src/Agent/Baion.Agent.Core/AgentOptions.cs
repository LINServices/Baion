namespace Baion.Agent.Core;

/// <summary>Configuración del agente.</summary>
public class AgentOptions
{
    /// <summary>Base del orquestador, con esquema ws:// o wss://.</summary>
    public string OrchestratorUri { get; set; } = "ws://localhost:5199";

    /// <summary>Token de instalación. Solo hace falta hasta que el agente obtiene su credencial permanente.</summary>
    public string? EnrollmentToken { get; set; }

    /// <summary>Carpeta donde se guarda el estado del agente. Vacío significa la ruta estándar de la plataforma.</summary>
    public string StateDirectory { get; set; } = string.Empty;

    public int MinReconnectSeconds { get; set; } = 1;

    public int MaxReconnectSeconds { get; set; } = 60;

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "Agent";
}
