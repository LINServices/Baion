using System.Text.Json;

namespace LIN.Cloud.Enterprise.Baion.Agent.Services.Implementations;

// Sobre con la que responden los scripts de PowerShell: o bien ok con data, o bien un fallo
// ya clasificado (not_found, forbidden, tool_failed). Data viene desconectada de su JsonDocument,
// así que sobrevive a que este se libere.
internal record PowerShellEnvelope(bool Ok, string? Code, string? Message, JsonElement Data)
{
    public const string NotFoundCode = "not_found";

    public const string ForbiddenCode = "forbidden";

    public const string ToolFailedCode = "tool_failed";
}
