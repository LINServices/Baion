using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Contracts.Services;

namespace LIN.Cloud.Enterprise.Baion.Agent.Services.Implementations;

// Traducción de la salida de systemd y journald a los DTOs del contrato.
internal static class SystemdParser
{
    // Propiedades que se le piden a systemctl show para armar el detalle.
    public const string ShowProperties = "Id,Description,LoadState,ActiveState,SubState,UnitFileState,MainPID,MemoryCurrent,FragmentPath,ExecMainStatus,ActiveEnterTimestamp,Requires";

    public static IReadOnlyList<SystemdUnit> ParseListUnits(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind is not JsonValueKind.Array)
            return [];

        return document.RootElement
            .EnumerateArray()
            .Where(element => !string.IsNullOrEmpty(GetString(element, "unit")))
            .Select(element => new SystemdUnit(GetString(element, "unit"), GetString(element, "active"), GetString(element, "sub"), GetString(element, "description")))
            .ToList();
    }

    public static IReadOnlyDictionary<string, string> ParseUnitFileStates(string json)
    {
        var states = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind is not JsonValueKind.Array)
            return states;

        var entries = document.RootElement
            .EnumerateArray()
            .Select(element => (File: GetString(element, "unit_file"), State: GetString(element, "state")))
            .Where(entry => !string.IsNullOrEmpty(entry.File));

        foreach (var (file, state) in entries)
            states[Path.GetFileName(file)] = state;

        return states;
    }

    public static Dictionary<string, string> ParseShow(string stdout)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(candidate => candidate.IndexOf('=') > 0))
        {
            var separator = line.IndexOf('=');
            properties[line[..separator].Trim()] = line[(separator + 1)..].Trim('\r', ' ');
        }

        return properties;
    }

    public static IReadOnlyList<ServiceLogLine> ParseJournal(string stdout)
    {
        return stdout
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseJournalLine)
            .OfType<ServiceLogLine>()
            .ToList();
    }

    // Devuelve null si la línea no es un objeto JSON válido.
    private static ServiceLogLine? ParseJournalLine(string raw)
    {
        var trimmed = raw.Trim();

        if (trimmed.Length == 0 || trimmed[0] != '{')
            return null;

        if (!TryParseDocument(trimmed, out var document))
            return null;

        using (document)
        {
            var root = document.RootElement;
            return new ServiceLogLine(ReadRealtime(root), ReadPriority(root), ReadMessage(root));
        }
    }

    public static ServiceRuntimeState MapRuntimeState(string? activeState)
    {
        return activeState switch
        {
            "active" or "reloading" => ServiceRuntimeState.Running,
            "activating" => ServiceRuntimeState.Starting,
            "deactivating" => ServiceRuntimeState.Stopping,
            "inactive" => ServiceRuntimeState.Stopped,
            "failed" => ServiceRuntimeState.Failed,
            _ => ServiceRuntimeState.Unknown
        };
    }

    public static ServiceStartupMode MapStartupMode(string? unitFileState)
    {
        return unitFileState switch
        {
            null or "" => ServiceStartupMode.Unknown,
            "enabled" or "enabled-runtime" or "alias" => ServiceStartupMode.Automatic,
            "disabled" => ServiceStartupMode.Disabled,
            "masked" or "masked-runtime" => ServiceStartupMode.Disabled,
            "static" or "indirect" or "generated" or "transient" => ServiceStartupMode.Manual,
            _ => ServiceStartupMode.Unknown
        };
    }

    public static string BuildRawState(string? activeState, string? subState)
    {
        var active = string.IsNullOrEmpty(activeState) ? "unknown" : activeState;

        return string.IsNullOrEmpty(subState) ? active : $"{active} ({subState})";
    }

    // ActiveEnterTimestamp viene como "Sat 2026-09-06 12:34:56 UTC" (o con otra zona). Se
    // descarta el día de la semana; si la zona no es UTC se asume la local, que en el agente coincide con
    // la del servidor.
    public static DateTimeOffset? ParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "n/a" || value.StartsWith('0'))
            return null;

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var weekdayOffset = parts.Length >= 3 && parts[0].Length == 3 && parts[0].All(char.IsLetter) ? 1 : 0;

        if (parts.Length < weekdayOffset + 2)
            return null;

        var core = string.Join(' ', parts.Skip(weekdayOffset).Take(2));
        var zoneIsUtc = parts.Length > weekdayOffset + 2 && parts[weekdayOffset + 2] is "UTC" or "GMT";
        var style = (zoneIsUtc ? DateTimeStyles.AssumeUniversal : DateTimeStyles.AssumeLocal) | DateTimeStyles.AdjustToUniversal;

        return DateTimeOffset.TryParse(core, CultureInfo.InvariantCulture, style, out var parsed) ? parsed : null;
    }

    public static int? ParsePositiveInt(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0 ? parsed : null;
    }

    // MemoryCurrent es "[not set]" o el máximo de un ulong cuando la contabilidad de memoria está apagada.
    public static long? ParseMemory(string? value)
    {
        if (!ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bytes) || bytes is ulong.MaxValue || bytes > long.MaxValue)
            return null;

        return (long)bytes;
    }

    // El último código de salida solo tiene sentido si el servicio no está corriendo ya.
    public static int? ParseExitCode(string? execMainStatus, string? activeState)
    {
        if (activeState is "active" or "activating" or "reloading")
            return null;

        return int.TryParse(execMainStatus, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    public static IReadOnlyList<string> ParseDependencies(string? requires)
    {
        if (string.IsNullOrWhiteSpace(requires))
            return [];

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
            return null;

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
            return string.Empty;

        if (value.ValueKind is JsonValueKind.String)
            return value.GetString() ?? string.Empty;

        // journald entrega los mensajes no-UTF8 como un array de bytes.
        if (value.ValueKind is JsonValueKind.Array)
        {
            var bytes = value.EnumerateArray().Where(item => item.TryGetByte(out _)).Select(item => item.GetByte()).ToArray();

            return Encoding.UTF8.GetString(bytes);
        }

        return value.ToString();
    }

    private static string GetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
    }
}
