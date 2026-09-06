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
/// Inspecciona y controla los servicios de Windows lanzando <c>powershell.exe</c> con scripts que emiten
/// una <see cref="PowerShellEnvelope"/> por stdout. El nombre del servicio se incrusta como literal
/// entrecomillado de PowerShell, nunca en una línea de shell.
/// </summary>
internal class WindowsServiceInspector(IOptions<ServiceInspectionOptions> options, ILogger<WindowsServiceInspector> logger) : IServiceInspector
{
    private const string PowerShell = "powershell.exe";

    public ServerPlatform Platform => ServerPlatform.Windows;

    public async Task<ServiceQueryOutcome<IReadOnlyList<ServiceSummary>>> ListAsync(string? nameFilter, CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(ListScript, "(listado)", cancellationToken);

        if (!result.Ok)
        {
            return Failure<IReadOnlyList<ServiceSummary>>(result);
        }

        var services = WindowsServiceParser.ParseSummaries(result.Data)
            .Where(summary => Matches(summary, nameFilter))
            .OrderBy(summary => summary.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ServiceQueryOutcome<IReadOnlyList<ServiceSummary>>.Ok(services);
    }

    public Task<ServiceQueryOutcome<ServiceDetail>> DescribeAsync(string serviceId, CancellationToken cancellationToken)
    {
        if (!IsPlausibleServiceName(serviceId))
        {
            return Task.FromResult(ServiceQueryOutcome<ServiceDetail>.NotFound($"El servicio '{serviceId}' no existe en la máquina."));
        }

        return DescribeCoreAsync(serviceId.Trim(), cancellationToken);
    }

    public async Task<ServiceQueryOutcome<ServiceLogPage>> GetLogsAsync(string serviceId, int maxLines, DateTimeOffset? since, CancellationToken cancellationToken)
    {
        if (!IsPlausibleServiceName(serviceId))
        {
            return ServiceQueryOutcome<ServiceLogPage>.NotFound($"El servicio '{serviceId}' no existe en la máquina.");
        }

        var result = await ExecuteAsync(LogsScript(serviceId.Trim(), maxLines, since), serviceId, cancellationToken);

        if (!result.Ok)
        {
            return Failure<ServiceLogPage>(result);
        }

        // Get-WinEvent entrega los sucesos de más nuevo a más viejo; el contrato los quiere al revés.
        var ordered = WindowsServiceParser.ParseLogLines(result.Data).Reverse().ToList();
        var (fitted, truncated) = LogBudget.Fit(ordered, options.Value.MaxLogPayloadBytes, alreadyTruncated: false);

        return ServiceQueryOutcome<ServiceLogPage>.Ok(new ServiceLogPage(fitted, truncated));
    }

    public async Task<ServiceQueryOutcome<ServiceDetail>> ControlAsync(string serviceId, ServiceControlAction action, CancellationToken cancellationToken)
    {
        if (!IsPlausibleServiceName(serviceId))
        {
            return ServiceQueryOutcome<ServiceDetail>.NotFound($"El servicio '{serviceId}' no existe en la máquina.");
        }

        var name = serviceId.Trim();
        var command = action switch
        {
            ServiceControlAction.Start => $"Start-Service -Name {Literal(name)}",
            ServiceControlAction.Stop => $"Stop-Service -Name {Literal(name)} -Force",
            ServiceControlAction.Restart => $"Restart-Service -Name {Literal(name)} -Force",
            ServiceControlAction.Enable => $"Set-Service -Name {Literal(name)} -StartupType Automatic",
            ServiceControlAction.Disable => $"Set-Service -Name {Literal(name)} -StartupType Disabled",
            _ => null
        };

        if (command is null)
        {
            return ServiceQueryOutcome<ServiceDetail>.ToolFailed($"La acción {action} no está soportada.");
        }

        var result = await ExecuteAsync(ControlScript(name, command), serviceId, cancellationToken);

        if (!result.Ok)
        {
            return Failure<ServiceDetail>(result);
        }

        logger.LogInformation("Acción {Accion} aplicada sobre el servicio {Servicio}", action, name);

        // El contrato pide el detalle ya con la acción aplicada.
        return await DescribeCoreAsync(name, cancellationToken);
    }

    private async Task<ServiceQueryOutcome<ServiceDetail>> DescribeCoreAsync(string name, CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(DescribeScript(name), name, cancellationToken);

        return result.Ok
            ? ServiceQueryOutcome<ServiceDetail>.Ok(WindowsServiceParser.ParseDetail(result.Data, name))
            : Failure<ServiceDetail>(result);
    }

    /// <summary>Lanza el script y valida la sobre. <see cref="ScriptResult.Data"/> solo es válido si <see cref="ScriptResult.Ok"/>.</summary>
    private async Task<ScriptResult> ExecuteAsync(string script, string serviceId, CancellationToken cancellationToken)
    {
        var run = await ProcessRunner.RunAsync(
            PowerShell,
            ["-NoProfile", "-NonInteractive", "-NoLogo", "-ExecutionPolicy", "Bypass", "-EncodedCommand", WindowsServiceParser.Encode(script)],
            TimeSpan.FromSeconds(Math.Max(options.Value.ToolTimeoutSeconds, 1)),
            cancellationToken);

        if (!run.Launched)
        {
            return ScriptResult.Fail(ServiceQueryFailedCode.ToolFailed, run.LaunchError!);
        }

        if (run.TimedOut)
        {
            return ScriptResult.Fail(ServiceQueryFailedCode.ToolFailed, $"PowerShell no respondió a tiempo al consultar '{serviceId}'.");
        }

        if (!WindowsServiceParser.TryParseEnvelope(run.StandardOutput, out var envelope))
        {
            var detail = string.IsNullOrWhiteSpace(run.StandardError) ? run.StandardOutput : run.StandardError;
            return ScriptResult.Fail(ServiceQueryFailedCode.ToolFailed, $"PowerShell devolvió una salida inesperada (código {run.ExitCode}): {detail.Trim()}");
        }

        if (envelope.Ok)
        {
            return ScriptResult.Success(envelope.Data);
        }

        var message = string.IsNullOrWhiteSpace(envelope.Message) ? $"No se pudo operar sobre '{serviceId}'." : envelope.Message!;
        var code = envelope.Code switch
        {
            PowerShellEnvelope.NotFoundCode => ServiceQueryFailedCode.NotFound,
            PowerShellEnvelope.ForbiddenCode => ServiceQueryFailedCode.Forbidden,
            _ => ServiceQueryFailedCode.ToolFailed
        };

        return ScriptResult.Fail(code, message);
    }

    private static ServiceQueryOutcome<T> Failure<T>(ScriptResult result) => result.FailureCode switch
    {
        ServiceQueryFailedCode.NotFound => ServiceQueryOutcome<T>.NotFound(result.FailureMessage!),
        ServiceQueryFailedCode.Forbidden => ServiceQueryOutcome<T>.Forbidden(result.FailureMessage!),
        _ => ServiceQueryOutcome<T>.ToolFailed(result.FailureMessage!)
    };

    private static bool Matches(ServiceSummary summary, string? nameFilter)
    {
        if (string.IsNullOrWhiteSpace(nameFilter))
        {
            return true;
        }

        return summary.Id.Contains(nameFilter, StringComparison.OrdinalIgnoreCase)
            || summary.DisplayName.Contains(nameFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlausibleServiceName(string serviceId)
    {
        if (string.IsNullOrWhiteSpace(serviceId) || serviceId.Length > 256)
        {
            return false;
        }

        return serviceId.All(character => !char.IsControl(character) && character is not ('/' or '\\'));
    }

    /// <summary>Nombre como literal de cadena de PowerShell: comillas simples con las internas dobladas.</summary>
    private static string Literal(string value) => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";

    private enum ServiceQueryFailedCode
    {
        NotFound,
        Forbidden,
        ToolFailed
    }

    private readonly record struct ScriptResult(bool Ok, JsonElement Data, ServiceQueryFailedCode FailureCode, string? FailureMessage)
    {
        public static ScriptResult Success(JsonElement data) => new(true, data, ServiceQueryFailedCode.ToolFailed, null);

        public static ScriptResult Fail(ServiceQueryFailedCode code, string message) => new(false, default, code, message);
    }

    private const string ListScript = """
        $ErrorActionPreference='Stop'
        try{
          $rows=@(Get-CimInstance -ClassName Win32_Service -ErrorAction Stop | ForEach-Object{
            [pscustomobject]@{name=$_.Name;displayName=$_.DisplayName;state=$_.State;startMode=$_.StartMode}
          })
          [pscustomobject]@{ok=$true;data=$rows}|ConvertTo-Json -Depth 4 -Compress
        }catch{
          [pscustomobject]@{ok=$false;code='tool_failed';message=[string]$_.Exception.Message}|ConvertTo-Json -Compress
        }
        """;

    private static string DescribeScript(string name) => $$"""
        $ErrorActionPreference='Stop'
        $name={{Literal(name)}}
        try{
          $svc=Get-CimInstance -ClassName Win32_Service -ErrorAction Stop | Where-Object { $_.Name -eq $name }
          if(-not $svc){
            [pscustomobject]@{ok=$false;code='not_found';message="El servicio '$name' no existe."}|ConvertTo-Json -Compress
            exit
          }
          $proc=$null
          if([int]$svc.ProcessId -gt 0){ $proc=Get-Process -Id ([int]$svc.ProcessId) -ErrorAction SilentlyContinue }
          $deps=@()
          try{ $deps=@((Get-Service -Name $name -ErrorAction Stop).ServicesDependedOn | ForEach-Object{ $_.Name }) }catch{}
          $data=[pscustomobject]@{
            name=$svc.Name
            displayName=$svc.DisplayName
            description=$svc.Description
            state=$svc.State
            startMode=$svc.StartMode
            processId=[int]$svc.ProcessId
            pathName=$svc.PathName
            startTime=$(if($proc){ $proc.StartTime.ToUniversalTime().ToString('o') }else{ $null })
            workingSet=$(if($proc){ [long]$proc.WorkingSet64 }else{ $null })
            dependencies=$deps
          }
          [pscustomobject]@{ok=$true;data=$data}|ConvertTo-Json -Depth 4 -Compress
        }catch{
          $msg=[string]$_.Exception.Message
          $code='tool_failed'
          if($msg -match 'Access is denied|PermissionDenied|requires elevation|Administrator'){ $code='forbidden' }
          elseif($msg -match 'Cannot find any service|was not found|does not exist'){ $code='not_found' }
          [pscustomobject]@{ok=$false;code=$code;message=$msg}|ConvertTo-Json -Compress
        }
        """;

    private static string LogsScript(string name, int maxLines, DateTimeOffset? since)
    {
        var max = Math.Max(maxLines, 1).ToString(CultureInfo.InvariantCulture);
        var sinceLiteral = since is { } from
            ? Literal(from.UtcDateTime.ToString("o", CultureInfo.InvariantCulture))
            : "$null";

        return $$"""
            $ErrorActionPreference='Stop'
            $name={{Literal(name)}}
            $max={{max}}
            $since={{sinceLiteral}}
            try{
              $svc=Get-CimInstance -ClassName Win32_Service -ErrorAction Stop | Where-Object { $_.Name -eq $name }
              if(-not $svc){
                [pscustomobject]@{ok=$false;code='not_found';message="El servicio '$name' no existe."}|ConvertTo-Json -Compress
                exit
              }
              $filter=@{ LogName='System'; ProviderName='Service Control Manager' }
              if($since){ $filter['StartTime']=[datetime]::Parse($since,[globalization.cultureinfo]::InvariantCulture,[globalization.datetimestyles]::RoundtripKind) }
              $rows=@()
              try{
                $rows=@(Get-WinEvent -FilterHashtable $filter -MaxEvents ($max*5) -ErrorAction Stop |
                  Where-Object { $_.Message -like "*$($svc.DisplayName)*" -or $_.Message -like "*$name*" } |
                  Select-Object -First $max |
                  ForEach-Object{ [pscustomobject]@{ timestamp=$_.TimeCreated.ToUniversalTime().ToString('o'); level=$_.LevelDisplayName; message=$_.Message } })
              }catch{
                if($_.Exception.Message -notmatch 'No events were found'){ throw }
              }
              [pscustomobject]@{ok=$true;data=$rows}|ConvertTo-Json -Depth 4 -Compress
            }catch{
              [pscustomobject]@{ok=$false;code='tool_failed';message=[string]$_.Exception.Message}|ConvertTo-Json -Compress
            }
            """;
    }

    private static string ControlScript(string name, string command) => $$"""
        $ErrorActionPreference='Stop'
        $name={{Literal(name)}}
        try{
          $svc=Get-CimInstance -ClassName Win32_Service -ErrorAction Stop | Where-Object { $_.Name -eq $name }
          if(-not $svc){
            [pscustomobject]@{ok=$false;code='not_found';message="El servicio '$name' no existe."}|ConvertTo-Json -Compress
            exit
          }
          {{command}}
          [pscustomobject]@{ok=$true;data='done'}|ConvertTo-Json -Compress
        }catch{
          $msg=[string]$_.Exception.Message
          $code='tool_failed'
          if($msg -match 'Access is denied|PermissionDenied|requires elevation|Administrator privilege|cannot be (started|stopped) due to'){ $code='forbidden' }
          elseif($msg -match 'Cannot find any service|was not found|does not exist'){ $code='not_found' }
          [pscustomobject]@{ok=$false;code=$code;message=$msg}|ConvertTo-Json -Compress
        }
        """;
}
