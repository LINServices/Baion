using System.Text.Json;

namespace Baion.Agent.Services.Implementations;

/// <summary>
/// Sobre con la que responden los scripts de PowerShell: o bien <c>ok</c> con <c>data</c>, o bien un fallo
/// ya clasificado (<c>not_found</c>, <c>forbidden</c>, <c>tool_failed</c>). <see cref="Data"/> viene
/// desconectada de su <see cref="JsonDocument"/>, así que sobrevive a que este se libere.
/// </summary>
internal record PowerShellEnvelope(bool Ok, string? Code, string? Message, JsonElement Data)
{
    public const string NotFoundCode = "not_found";

    public const string ForbiddenCode = "forbidden";

    public const string ToolFailedCode = "tool_failed";
}
