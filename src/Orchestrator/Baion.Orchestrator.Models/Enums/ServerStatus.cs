namespace Baion.Orchestrator.Models.Enums;

/// <summary>Estado de conexión del servidor frente al orquestador.</summary>
public enum ServerStatus
{
    Provisioning = 1,
    Online = 2,
    Offline = 3,
    Disabled = 4
}
