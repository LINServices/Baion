namespace Baion.Cliente.Web;

/// <summary>Cómo llega el panel al orquestador.</summary>
public class BaionApiOptions
{
    /// <summary>Base de la API, por ejemplo <c>http://localhost:5199</c>.</summary>
    public string BaseAddress { get; set; } = "http://localhost:5199";

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "BaionApi";
}
