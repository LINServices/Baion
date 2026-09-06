namespace Baion.Contracts.Enums;

/// <summary>Acción que el orquestador pide aplicar sobre un servicio del sistema.</summary>
public enum ServiceControlAction
{
    Start = 1,
    Stop = 2,
    Restart = 3,
    Enable = 4,
    Disable = 5
}
