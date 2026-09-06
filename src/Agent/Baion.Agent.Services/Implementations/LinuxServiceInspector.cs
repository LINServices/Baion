using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Agent.Services.Implementations;

/// <summary>
/// Inspecciona y controla los servicios de un sistema con systemd: <c>systemctl</c> para el estado y las
/// acciones, <c>journalctl</c> para los logs. Ningún identificador se interpola en una shell: todo va como
/// argumento suelto a través de <see cref="ProcessRunner"/>.
/// </summary>
internal class LinuxServiceInspector(IOptions<ServiceInspectionOptions> options, ILogger<LinuxServiceInspector> logger) : IServiceInspector
{
    private const string Systemctl = "systemctl";

    private const string Journalctl = "journalctl";

    public ServerPlatform Platform => ServerPlatform.Linux;

    public async Task<ServiceQueryOutcome<IReadOnlyList<ServiceSummary>>> ListAsync(string? nameFilter, CancellationToken cancellationToken)
    {
        var units = await RunAsync(["list-units", "--type=service", "--all", "--no-pager", "--no-legend", "--output=json"], cancellationToken);

        if (!units.Succeeded)
        {
            return Classify<IReadOnlyList<ServiceSummary>>("(listado)", units);
        }

        var files = await RunAsync(["list-unit-files", "--type=service", "--no-pager", "--no-legend", "--output=json"], cancellationToken);
        var startupModes = files.Succeeded ? SafeParseUnitFileStates(files.StandardOutput) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var summaries = SafeParseListUnits(units.StandardOutput)
            .Select(unit => new ServiceSummary(
                unit.Unit,
                string.IsNullOrWhiteSpace(unit.Description) ? unit.Unit : unit.Description,
                SystemdParser.MapRuntimeState(unit.Active),
                SystemdParser.BuildRawState(unit.Active, unit.Sub),
                SystemdParser.MapStartupMode(startupModes.GetValueOrDefault(unit.Unit))))
            .Where(summary => Matches(summary, nameFilter))
            .OrderBy(summary => summary.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ServiceQueryOutcome<IReadOnlyList<ServiceSummary>>.Ok(summaries);
    }

    public async Task<ServiceQueryOutcome<ServiceDetail>> DescribeAsync(string serviceId, CancellationToken cancellationToken)
    {
        if (!IsPlausibleUnitName(serviceId))
        {
            return ServiceQueryOutcome<ServiceDetail>.NotFound($"El servicio '{serviceId}' no existe en la máquina.");
        }

        var unit = NormalizeUnitName(serviceId);
        var show = await RunAsync(["show", unit, "--no-pager", $"--property={SystemdParser.ShowProperties}"], cancellationToken);

        if (!show.Succeeded)
        {
            return Classify<ServiceDetail>(serviceId, show);
        }

        var properties = SystemdParser.ParseShow(show.StandardOutput);

        if (properties.GetValueOrDefault("LoadState") is "not-found" or "error" or "bad-setting")
        {
            return ServiceQueryOutcome<ServiceDetail>.NotFound($"El servicio '{serviceId}' no existe en la máquina.");
        }

        return ServiceQueryOutcome<ServiceDetail>.Ok(BuildDetail(unit, properties));
    }

    public async Task<ServiceQueryOutcome<ServiceLogPage>> GetLogsAsync(string serviceId, int maxLines, DateTimeOffset? since, CancellationToken cancellationToken)
    {
        if (!IsPlausibleUnitName(serviceId))
        {
            return ServiceQueryOutcome<ServiceLogPage>.NotFound($"El servicio '{serviceId}' no existe en la máquina.");
        }

        var unit = NormalizeUnitName(serviceId);
        var arguments = new List<string> { "-u", unit, "--no-pager", "--output=json", "--utc", "-n", maxLines.ToString(CultureInfo.InvariantCulture) };

        if (since is { } from)
        {
            arguments.Add("--since");
            arguments.Add(from.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        }

        var journal = await RunAsync(Journalctl, arguments, cancellationToken);

        if (!journal.Succeeded)
        {
            return Classify<ServiceLogPage>(serviceId, journal);
        }

        var lines = SystemdParser.ParseJournal(journal.StandardOutput);
        var (fitted, truncated) = LogBudget.Fit(lines, options.Value.MaxLogPayloadBytes, alreadyTruncated: false);

        return ServiceQueryOutcome<ServiceLogPage>.Ok(new ServiceLogPage(fitted, truncated));
    }

    public async Task<ServiceQueryOutcome<ServiceDetail>> ControlAsync(string serviceId, ServiceControlAction action, CancellationToken cancellationToken)
    {
        if (!IsPlausibleUnitName(serviceId))
        {
            return ServiceQueryOutcome<ServiceDetail>.NotFound($"El servicio '{serviceId}' no existe en la máquina.");
        }

        var unit = NormalizeUnitName(serviceId);
        var verb = action switch
        {
            ServiceControlAction.Start => "start",
            ServiceControlAction.Stop => "stop",
            ServiceControlAction.Restart => "restart",
            ServiceControlAction.Enable => "enable",
            ServiceControlAction.Disable => "disable",
            _ => null
        };

        if (verb is null)
        {
            return ServiceQueryOutcome<ServiceDetail>.ToolFailed($"La acción {action} no está soportada.");
        }

        var result = await RunAsync(["--no-pager", verb, unit], cancellationToken);

        if (!result.Succeeded)
        {
            return Classify<ServiceDetail>(serviceId, result);
        }

        logger.LogInformation("Acción {Accion} aplicada sobre la unidad {Unidad}", verb, unit);

        // El contrato pide el detalle ya con la acción aplicada.
        return await DescribeAsync(unit, cancellationToken);
    }

    private ServiceDetail BuildDetail(string unit, IReadOnlyDictionary<string, string> properties)
    {
        var activeState = properties.GetValueOrDefault("ActiveState");
        var subState = properties.GetValueOrDefault("SubState");
        var description = properties.GetValueOrDefault("Description");
        var id = properties.GetValueOrDefault("Id") is { Length: > 0 } shownId ? shownId : unit;

        return new ServiceDetail(
            id,
            string.IsNullOrWhiteSpace(description) ? id : description,
            string.IsNullOrWhiteSpace(description) ? null : description,
            SystemdParser.MapRuntimeState(activeState),
            SystemdParser.BuildRawState(activeState, subState),
            string.IsNullOrWhiteSpace(subState) ? null : subState,
            SystemdParser.MapStartupMode(properties.GetValueOrDefault("UnitFileState")),
            SystemdParser.ParsePositiveInt(properties.GetValueOrDefault("MainPID")),
            SystemdParser.ParseTimestamp(properties.GetValueOrDefault("ActiveEnterTimestamp")),
            SystemdParser.ParseMemory(properties.GetValueOrDefault("MemoryCurrent")),
            properties.GetValueOrDefault("FragmentPath") is { Length: > 0 } fragment ? fragment : null,
            SystemdParser.ParseExitCode(properties.GetValueOrDefault("ExecMainStatus"), activeState),
            SystemdParser.ParseDependencies(properties.GetValueOrDefault("Requires")));
    }

    private Task<ProcessRunResult> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken) => RunAsync(Systemctl, arguments, cancellationToken);

    private Task<ProcessRunResult> RunAsync(string tool, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        => ProcessRunner.RunAsync(tool, arguments, TimeSpan.FromSeconds(Math.Max(options.Value.ToolTimeoutSeconds, 1)), cancellationToken);

    private static bool Matches(ServiceSummary summary, string? nameFilter)
    {
        if (string.IsNullOrWhiteSpace(nameFilter))
        {
            return true;
        }

        return summary.Id.Contains(nameFilter, StringComparison.OrdinalIgnoreCase)
            || summary.DisplayName.Contains(nameFilter, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Una unidad sin sufijo se entiende como <c>.service</c>, igual que hace <c>systemctl</c>.</summary>
    private static string NormalizeUnitName(string serviceId)
    {
        var trimmed = serviceId.Trim();

        return trimmed.Contains('.', StringComparison.Ordinal) ? trimmed : $"{trimmed}.service";
    }

    /// <summary>
    /// Descarta de entrada nombres que systemd nunca aceptaría: así un identificador basura se resuelve como
    /// «no existe» sin llegar a lanzar el proceso.
    /// </summary>
    private static bool IsPlausibleUnitName(string serviceId)
    {
        if (string.IsNullOrWhiteSpace(serviceId) || serviceId.Length > 256)
        {
            return false;
        }

        return serviceId.All(character => !char.IsControl(character) && character is not (' ' or '/' or '\\'));
    }

    private IReadOnlyList<SystemdUnit> SafeParseListUnits(string json)
    {
        try
        {
            return SystemdParser.ParseListUnits(json);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "No se pudo interpretar la salida de systemctl list-units");
            return [];
        }
    }

    private IReadOnlyDictionary<string, string> SafeParseUnitFileStates(string json)
    {
        try
        {
            return SystemdParser.ParseUnitFileStates(json);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "No se pudo interpretar la salida de systemctl list-unit-files");
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static ServiceQueryOutcome<T> Classify<T>(string serviceId, ProcessRunResult result)
    {
        if (!result.Launched)
        {
            return ServiceQueryOutcome<T>.ToolFailed(result.LaunchError!);
        }

        if (result.TimedOut)
        {
            return ServiceQueryOutcome<T>.ToolFailed($"La herramienta del sistema no respondió a tiempo al consultar '{serviceId}'.");
        }

        var text = $"{result.StandardError}\n{result.StandardOutput}";

        if (Mentions(text, "could not be found") || Mentions(text, "not found") || Mentions(text, "no such file") || Mentions(text, "not-found"))
        {
            return ServiceQueryOutcome<T>.NotFound($"El servicio '{serviceId}' no existe en la máquina.");
        }

        if (Mentions(text, "interactive authentication required")
            || Mentions(text, "access denied")
            || Mentions(text, "permission denied")
            || Mentions(text, "must be root")
            || Mentions(text, "not authorized")
            || Mentions(text, "authentication is required"))
        {
            return ServiceQueryOutcome<T>.Forbidden($"El agente no tiene permisos para operar sobre '{serviceId}'.");
        }

        var detail = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;

        return ServiceQueryOutcome<T>.ToolFailed($"systemctl devolvió el código {result.ExitCode}: {detail.Trim()}");
    }

    private static bool Mentions(string haystack, string needle) => haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
