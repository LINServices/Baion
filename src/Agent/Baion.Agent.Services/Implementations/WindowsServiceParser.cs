using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Baion.Contracts.Enums;
using Baion.Contracts.Services;

namespace Baion.Agent.Services.Implementations;

/// <summary>Traducción de la salida de los scripts de PowerShell a los DTOs del contrato.</summary>
internal static class WindowsServiceParser
{
    /// <summary>Codifica el script como espera <c>powershell.exe -EncodedCommand</c>: base64 de UTF-16LE.</summary>
    public static string Encode(string script) => Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

    public static bool TryParseEnvelope(string stdout, out PowerShellEnvelope envelope)
    {
        envelope = new PowerShellEnvelope(false, PowerShellEnvelope.ToolFailedCode, null, default);

        var trimmed = stdout.Trim();

        if (trimmed.Length == 0)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            var root = document.RootElement;

            if (root.ValueKind is not JsonValueKind.Object)
            {
                return false;
            }

            var ok = root.TryGetProperty("ok", out var okElement) && okElement.ValueKind is JsonValueKind.True;
            var code = root.TryGetProperty("code", out var codeElement) && codeElement.ValueKind is JsonValueKind.String ? codeElement.GetString() : null;
            var message = root.TryGetProperty("message", out var messageElement) && messageElement.ValueKind is JsonValueKind.String ? messageElement.GetString() : null;
            var data = root.TryGetProperty("data", out var dataElement) ? dataElement.Clone() : default;

            envelope = new PowerShellEnvelope(ok, code, message, data);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static IReadOnlyList<ServiceSummary> ParseSummaries(JsonElement data)
    {
        var summaries = new List<ServiceSummary>();

        foreach (var element in Enumerate(data))
        {
            var name = GetString(element, "name");

            if (name.Length == 0)
            {
                continue;
            }

            var rawState = GetString(element, "state");
            var startMode = GetString(element, "startMode");

            summaries.Add(new ServiceSummary(
                name,
                GetString(element, "displayName") is { Length: > 0 } displayName ? displayName : name,
                MapState(rawState),
                rawState.Length == 0 ? "unknown" : rawState,
                MapStartupMode(startMode)));
        }

        return summaries;
    }

    public static ServiceDetail ParseDetail(JsonElement data, string fallbackId)
    {
        var name = GetString(data, "name") is { Length: > 0 } shown ? shown : fallbackId;
        var rawState = GetString(data, "state");
        var description = GetString(data, "description");
        var path = GetString(data, "pathName");

        return new ServiceDetail(
            name,
            GetString(data, "displayName") is { Length: > 0 } displayName ? displayName : name,
            description.Length == 0 ? null : description,
            MapState(rawState),
            rawState.Length == 0 ? "unknown" : rawState,
            null,
            MapStartupMode(GetString(data, "startMode")),
            GetPositiveInt(data, "processId"),
            GetTimestamp(data, "startTime"),
            GetLong(data, "workingSet"),
            path.Length == 0 ? null : path,
            GetNullableInt(data, "exitCode"),
            GetStringArray(data, "dependencies"));
    }

    public static IReadOnlyList<ServiceLogLine> ParseLogLines(JsonElement data)
    {
        var lines = new List<ServiceLogLine>();

        foreach (var element in Enumerate(data))
        {
            lines.Add(new ServiceLogLine(
                GetTimestamp(element, "timestamp"),
                GetString(element, "level") is { Length: > 0 } level ? level : null,
                GetString(element, "message")));
        }

        return lines;
    }

    public static ServiceRuntimeState MapState(string? state) => state switch
    {
        "Running" => ServiceRuntimeState.Running,
        "Stopped" => ServiceRuntimeState.Stopped,
        "Start Pending" => ServiceRuntimeState.Starting,
        "Stop Pending" => ServiceRuntimeState.Stopping,
        "Paused" or "Pause Pending" or "Continue Pending" => ServiceRuntimeState.Stopped,
        _ => ServiceRuntimeState.Unknown
    };

    public static ServiceStartupMode MapStartupMode(string? startMode) => startMode switch
    {
        "Auto" or "Automatic" => ServiceStartupMode.Automatic,
        "Manual" => ServiceStartupMode.Manual,
        "Disabled" => ServiceStartupMode.Disabled,
        "Boot" or "System" => ServiceStartupMode.Automatic,
        _ => ServiceStartupMode.Unknown
    };

    private static IEnumerable<JsonElement> Enumerate(JsonElement data)
    {
        if (data.ValueKind is JsonValueKind.Array)
        {
            foreach (var element in data.EnumerateArray())
            {
                yield return element;
            }
        }
        else if (data.ValueKind is JsonValueKind.Object)
        {
            yield return data;
        }
    }

    private static string GetString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return string.Empty;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.ToString(),
            _ => string.Empty
        };
    }

    private static int? GetPositiveInt(JsonElement element, string property)
        => GetNullableInt(element, property) is { } value && value > 0 ? value : null;

    private static int? GetNullableInt(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static long? GetLong(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out var number) && number > 0 => number,
            JsonValueKind.String when long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0 => parsed,
            _ => null
        };
    }

    private static DateTimeOffset? GetTimestamp(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind is not JsonValueKind.String)
        {
            return null;
        }

        return DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) ? parsed : null;
    }

    private static IReadOnlyList<string> GetStringArray(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind is not JsonValueKind.Array)
        {
            return [];
        }

        var items = new List<string>();

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind is JsonValueKind.String && item.GetString() is { Length: > 0 } text)
            {
                items.Add(text);
            }
        }

        return items;
    }
}
