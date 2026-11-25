using System.Text.Json;

namespace LIN.Cloud.Enterprise.Baion.Agent.Services;

internal readonly record struct ScriptResult(bool Ok, JsonElement Data, ServiceQueryFailedCode FailureCode, string? FailureMessage)
{
    public static ScriptResult Success(JsonElement data)
    {
        return new ScriptResult(true, data, ServiceQueryFailedCode.ToolFailed, null);
    }

    public static ScriptResult Fail(ServiceQueryFailedCode code, string message)
    {
        return new ScriptResult(false, default, code, message);
    }
}
