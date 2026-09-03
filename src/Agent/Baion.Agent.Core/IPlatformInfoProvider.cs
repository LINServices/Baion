using Baion.Contracts.Enums;

namespace Baion.Agent.Core;

/// <summary>Datos de la máquina que el agente comunica en el saludo.</summary>
public interface IPlatformInfoProvider
{
    ServerPlatform Platform { get; }

    /// <summary>RID del runtime en curso; determina qué binario descargar en una auto-actualización.</summary>
    string RuntimeIdentifier { get; }

    string AgentVersion { get; }

    string Hostname { get; }

    int CoreCount { get; }

    long TotalMemoryBytes { get; }
}
