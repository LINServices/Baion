using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

public class OrchestratorOptions
{
    public const string SectionName = "Orchestrator";

    // Se persiste en cada servidor conectado para saber qué instancia tiene el socket abierto.
    public string InstanceId { get; set; } = $"{Environment.MachineName}-{Environment.ProcessId}";

    // Cadencia con la que el agente debe enviar su latido.
    public int HeartbeatSeconds { get; set; } = 30;

    // Margen para que el agente envíe su saludo tras abrirse el socket.
    public int HandshakeTimeoutSeconds { get; set; } = 10;
}
