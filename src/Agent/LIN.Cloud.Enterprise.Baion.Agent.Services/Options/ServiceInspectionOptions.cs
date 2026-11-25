namespace LIN.Cloud.Enterprise.Baion.Agent.Services;

public class ServiceInspectionOptions
{
    public const string SectionName = "Services";

    // Plazo para que una herramienta del sistema (systemctl, journalctl, powershell) responda.
    public int ToolTimeoutSeconds { get; set; } = 15;

    // Presupuesto de bytes para las líneas de una respuesta de logs. La trama del protocolo son 1 MB
    // (BaionMessageChannel.DefaultMaxMessageBytes); se deja margen para el resto del mensaje. Al
    // superarlo, el agente recorta las líneas más antiguas y marca la respuesta como truncada.
    public int MaxLogPayloadBytes { get; set; } = 768 * 1024;
}
