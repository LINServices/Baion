using System.Text.Json;
using System.Text.Json.Serialization;

namespace Baion.Contracts;

/// <summary>Constantes y configuración de serialización compartidas por el orquestador y el agente.</summary>
public static class BaionProtocol
{
    /// <summary>Versión del protocolo que se negocia en el handshake.</summary>
    public const string Version = "1.0";

    /// <summary>Ruta del endpoint WebSocket al que se conecta el agente.</summary>
    public const string WebSocketPath = "/ws/agent";

    /// <summary>Cabecera con el token de instalación; solo se usa en el enrolamiento inicial.</summary>
    public const string EnrollmentTokenHeader = "X-Baion-Enrollment-Token";

    /// <summary>Cabecera con la credencial permanente del agente, usada en cada reconexión.</summary>
    public const string AgentTokenHeader = "X-Baion-Agent-Token";

    /// <summary>Cabecera con la versión de protocolo que habla el agente.</summary>
    public const string ProtocolVersionHeader = "X-Baion-Protocol-Version";

    /// <summary>Opciones de serialización del protocolo. Ambos extremos deben usar exactamente estas.</summary>
    public static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
