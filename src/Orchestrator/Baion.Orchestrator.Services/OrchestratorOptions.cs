using System;

namespace Baion.Orchestrator.Services;

/// <summary>Parámetros de esta instancia del orquestador.</summary>
public class OrchestratorOptions
{
    /// <summary>
    /// Identifica a esta instancia entre las que atienden agentes. Se persiste en cada servidor conectado
    /// para saber qué instancia tiene el socket abierto.
    /// </summary>
    public string InstanceId { get; set; } = $"{Environment.MachineName}-{Environment.ProcessId}";

    /// <summary>Cadencia con la que el agente debe enviar su latido.</summary>
    public int HeartbeatSeconds { get; set; } = 30;

    /// <summary>Margen para que el agente envíe su saludo tras abrirse el socket.</summary>
    public int HandshakeTimeoutSeconds { get; set; } = 10;

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "Orchestrator";
}
