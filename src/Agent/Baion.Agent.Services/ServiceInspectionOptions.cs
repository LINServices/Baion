namespace Baion.Agent.Services;

/// <summary>Configuración de la inspección y el control de servicios del sistema.</summary>
public class ServiceInspectionOptions
{
    /// <summary>Plazo para que una herramienta del sistema (systemctl, journalctl, powershell) responda.</summary>
    public int ToolTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Presupuesto de bytes para las líneas de una respuesta de logs. La trama del protocolo son 1 MB
    /// (<c>BaionMessageChannel.DefaultMaxMessageBytes</c>); se deja margen para el resto del mensaje. Al
    /// superarlo, el agente recorta las líneas más antiguas y marca la respuesta como truncada.
    /// </summary>
    public int MaxLogPayloadBytes { get; set; } = 768 * 1024;

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "Services";
}
