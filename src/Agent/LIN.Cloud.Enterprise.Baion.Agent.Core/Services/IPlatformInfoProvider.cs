using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Agent.Core;

/// <summary>
/// Datos de la máquina que el agente comunica en el saludo.
/// </summary>
public interface IPlatformInfoProvider
{
    /// <summary>
    /// Sistema operativo en el que corre el agente.
    /// </summary>
    ServerPlatform Platform { get; }

    /// <summary>
    /// RID del runtime en curso; determina qué binario descargar en una auto-actualización.
    /// </summary>
    string RuntimeIdentifier { get; }

    /// <summary>
    /// Versión informativa del ensamblado del agente.
    /// </summary>
    string AgentVersion { get; }

    /// <summary>
    /// Nombre de la máquina.
    /// </summary>
    string Hostname { get; }

    /// <summary>
    /// Núcleos lógicos visibles para el runtime.
    /// </summary>
    int CoreCount { get; }

    /// <summary>
    /// Memoria total disponible para el runtime, en bytes.
    /// </summary>
    long TotalMemoryBytes { get; }
}
