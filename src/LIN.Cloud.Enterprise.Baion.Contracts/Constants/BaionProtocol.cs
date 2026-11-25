using System.Text.Json;
using System.Text.Json.Serialization;

namespace LIN.Cloud.Enterprise.Baion.Contracts;

public static class BaionProtocol
{
    public const string Version = "1.0";

    public const string WebSocketPath = "/ws/agent";

    // Token de instalación: solo se usa en el enrolamiento inicial
    public const string EnrollmentTokenHeader = "X-Baion-Enrollment-Token";

    // Credencial permanente del agente, usada en cada reconexión
    public const string AgentTokenHeader = "X-Baion-Agent-Token";

    public const string ProtocolVersionHeader = "X-Baion-Protocol-Version";

    // Ambos extremos deben usar exactamente estas opciones
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
