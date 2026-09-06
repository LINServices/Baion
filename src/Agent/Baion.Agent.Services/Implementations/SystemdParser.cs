using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Baion.Contracts.Enums;
using Baion.Contracts.Services;

namespace Baion.Agent.Services.Implementations;

/// <summary>Traducción de la salida de systemd y journald a los DTOs del contrato.</summary>
internal static class SystemdParser
{
    /// <summary>Propiedades que se le piden a <c>systemctl show</c> para armar el detalle.</summary>
    public const string ShowProperties = "Id,Description,LoadState,ActiveState,SubState,UnitFileState,MainPID,MemoryCurrent,FragmentPath,ExecMainStatus,ActiveEnterTimestamp,Requires";

    public static IReadOnlyList<SystemdUnit> ParseListUnits(string json)
    {
        var units = new List<SystemdUnit>();

        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind is not JsonValueKind.Array)
        {
            return units;
        }

        foreach (var element in document.RootElement.EnumerateArray())
        {
            var unit = GetString(element, "unit");

            if (!string.IsNullOrEmpty(unit))
            {
                units.Add(new SystemdUnit(unit, GetString(element, "active"), GetString(element, "sub"), GetString(element, "description")));
            }
        }

        return units;
    }

    public static IReadOnlyDictionary<string, string> ParseUnitFileStates(string json)
    {
        var states = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind is not JsonValueKind.Array)
        {
            return states;
        }

        foreach (var element in document.RootElement.EnumerateArray())
        {
            var unit = GetString(element, "unit_file");

            if (!string.IsNullOrEmpty(unit))
            {
                states[Path.GetFileName(unit)] = GetString(element, "state");
            }
        }

        return states;
    }

    public static Dictionary<string, string> ParseShow(string stdout)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = line.IndexOf('=');

            if (separator > 0)
            {
                properties[line[..separator].Trim()] = line[(separator + 1)..].Trim('\r', ' ');
            }
        }

        return properties;
    }

    public static IReadOnlyList<ServiceLogLine> ParseJournal(string stdout)
    {
        var lines = new List<ServiceLogLine>();

        foreach (var raw in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = raw.Trim();

            if (trimmed.Length == 0 || trimmed[0] != '{')
            {
                continue;
            }

            if (!TryParseDocument(trimmed, out var document))
            {
                continue;
            }

            using (document)
            {
                var root = document.RootElement;
                lines.Add(new ServiceLogLine(ReadRealtime(root), ReadPriority(root), ReadMessage(root)));
            }
        }

        return lines;
    }

    public static ServiceRuntimeState MapRuntimeState(string? activeState) => activeState switch
    {
        "active" or "reloading" => ServiceRuntimeState.Running,
        "activating" => ServiceRuntimeState.Starting,
        "deactivating" => ServiceRuntimeState.Stopping,
        "inactive" => ServiceRuntimeState.Stopped,
        "failed" => ServiceRuntimeState.Failed,
        _ => ServiceRuntimeState.Unknown
    };

    public static ServiceStartupMode MapStartupMode(string? unitFileState) => unitFileState switch
    {
        null or "" => ServiceStartupMode.Unknown,
        "enabled" or "enabled-runtime" or "alias" => ServiceStartupMode.Automatic,
        "disabled" => ServiceStartupMode.Disabled,
        "masked" or "masked-runtime" => ServiceStartupMode.Disabled,
        "static" or "indirect" or "generated" or "transient" => ServiceStartupMode.Manual,
        _ => ServiceStartupMode.Unknown
    };

    public static string BuildRawState(string? activeState, string? subState)
    {
        var active = string.IsNullOrEmpty(activeState) ? "unknown" : activeState;

        return string.IsNullOrEmpty(subState) ? active : $"{active} ({subState})";
    }

    /// <summary>
    /// <c>ActiveEnterTimestamp</c> viene como <c>Sat 2026-09-06 12:34:56 UTC</c> (o con otra zona). Se
    /// descarta el día de la semana; si la zona no es UTC se asume la local, que en el agente coincide con
    /// la del servidor.
    /// </summary>
    public static DateTimeOffset? ParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "n/a" || value.StartsWith('0'))
        {
            return null;
        }

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var weekdayOffset = parts.Length >= 3 && parts[0].Length == 3 && parts[0].All(char.IsLetter) ? 1 : 0;

        if (parts.Length < weekdayOffset + 2)
        {
            return null;
        }

        var core = string.Join(' ', parts.Skip(weekdayOffset).Take(2));
        var zoneIsUtc = parts.Length > weekdayOffset + 2 && parts[weekdayOffset + 2] is "UTC" or "GMT";
        var style = (zoneIsUtc ? DateTimeStyles.AssumeUniversal : DateTimeStyles.AssumeLocal) | DateTimeStyles.AdjustToUniversal;

        return DateTimeOffset.TryParse(core, CultureInfo.InvariantCulture, style, out var parsed) ? parsed : null;
    }

    public static int? ParsePositiveInt(string? value) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0 ? parsed : null;

    /// <summary><c>MemoryCurrent</c> es <c>[not set]</c> o el máximo de un ulong cuando la contabilidad de memoria está apagada.</summary>
    public static long? ParseMemory(string? value)
    {
        if (!ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bytes) || bytes is ulong.MaxValue || bytes > long.MaxValue)
        {
            return null;
        }

        return (long)bytes;
    }

    /// <summary>El último código de salida solo tiene sentido si el servicio no está corriendo ya.</summary>
    public static int? ParseExitCode(string? execMainStatus, string? activeState)
    {
        if (activeState is "active" or "activating" or "reloading")
        {
            return null;
        }

        return int.TryParse(execMainStatus, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    public static IReadOnlyList<string> ParseDependencies(string? requires)
    {
        if (string.IsNullOrWhiteSpace(requires))
        {
            return [];
        }

        return requires
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(dependency => dependency.EndsWith(".service", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static bool TryParseDocument(string json, out JsonDocument document)
    {
        try
        {
            document = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            document = null!;
            return false;
        }
    }

    private static DateTimeOffset? ReadRealtime(JsonElement root)
    {
        if (root.TryGetProperty("__REALTIME_TIMESTAMP", out var value)
            && value.ValueKind is JsonValueKind.String
            && long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var micros))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(micros / 1000);
        }

        return null;
    }

    private static string? ReadPriority(JsonElement root)
    {
        if (!root.TryGetProperty("PRIORITY", out var value))
        {
            return null;
        }

        var priority = value.ValueKind is JsonValueKind.String ? value.GetString() : value.ToString();

        return priority switch
        {
            "0" => "emerg",
            "1" => "alert",
            "2" => "crit",
            "3" => "error",
            "4" => "warning",
            "5" => "notice",
            "6" => "info",
            "7" => "debug",
            _ => priority
        };
    }

    private static string ReadMessage(JsonElement root)
    {
        if (!root.TryGetProperty("MESSAGE", out var value))
        {
            return string.Empty;
        }

        if (value.ValueKind is JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        // journald entrega los mensajes no-UTF8 como un array de bytes.
        if (value.ValueKind is JsonValueKind.Array)
        {
            var bytes = new List<byte>();

            foreach (var item in value.EnumerateArray())
            {
                if (item.TryGetByte(out var singleByte))
                {
                    bytes.Add(singleByte);
                }
            }

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        return value.ToString();
    }

    private static string GetString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
}
