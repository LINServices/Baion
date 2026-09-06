namespace Baion.Contracts.Enums;

/// <summary>Cómo arranca un servicio al iniciar la máquina.</summary>
public enum ServiceStartupMode
{
    Unknown = 0,
    Automatic = 1,
    Manual = 2,
    Disabled = 3
}
