namespace Baion.Contracts.Enums;

/// <summary>Estado de ejecución de un servicio del sistema, normalizado entre systemd y el SCM de Windows.</summary>
public enum ServiceRuntimeState
{
    Unknown = 0,
    Running = 1,
    Stopped = 2,
    Starting = 3,
    Stopping = 4,
    Failed = 5
}
