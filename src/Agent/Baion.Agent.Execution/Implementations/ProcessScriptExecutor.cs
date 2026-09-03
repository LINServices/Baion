using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Contracts.Enums;
using Baion.Contracts.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Agent.Execution.Implementations;

/// <summary>
/// Todo lo que comparten los ejecutores de las dos plataformas: materializar el script, lanzar el proceso,
/// ir sacando su salida y terminarlo si se pasa del tiempo. Lo único que cambia por plataforma es el
/// intérprete que se invoca y cómo se le pasa el archivo.
/// </summary>
internal abstract class ProcessScriptExecutor(IOptions<ScriptExecutionOptions> options, ILogger logger) : IScriptExecutor
{
    public abstract ServerPlatform Platform { get; }

    public async Task<ScriptExecutionOutcome> ExecuteAsync(ExecuteScriptMessage request, Func<int, Task> onStarted, Func<OutputStream, string, Task> onOutput, CancellationToken cancellationToken)
    {
        if (!ScriptRuntimeCompatibility.IsSupported(request.Runtime, Platform))
        {
            return ScriptExecutionOutcome.Rejected($"El intérprete {request.Runtime} no puede ejecutarse en {Platform}.");
        }

        // Se verifica antes de escribir nada: un contenido alterado en tránsito no llega a tocar el disco.
        if (!ChecksumMatches(request))
        {
            return ScriptExecutionOutcome.Rejected("El checksum del script no coincide con su contenido.");
        }

        var directory = ExecutionWorkspace.CreateFor(options.Value, request.ExecutionId);
        var conservar = false;

        try
        {
            var scriptPath = await WriteScriptAsync(directory, request, cancellationToken);
            var outcome = await RunAsync(request, scriptPath, onStarted, onOutput, cancellationToken);

            // En Detached el proceso sigue vivo y necesita su script en disco: la carpeta se conserva
            // y la recoge el barrido por antigüedad del próximo arranque.
            conservar = request.Mode is ExecutionMode.Detached && outcome.Status is ExecutionStatus.Succeeded;

            return outcome;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Falló la ejecución {ExecutionId}", request.ExecutionId);
            return ScriptExecutionOutcome.Rejected($"No se pudo ejecutar el script: {exception.Message}");
        }
        finally
        {
            if (!conservar)
            {
                ExecutionWorkspace.TryDelete(directory, logger);
            }
        }
    }

    /// <summary>Intérprete y argumentos con los que se lanza el script ya materializado.</summary>
    protected abstract (string FileName, string Arguments) BuildCommand(ScriptRuntime runtime, string scriptPath);

    /// <summary>Extensión del archivo temporal; en Windows determina cómo lo trata el intérprete.</summary>
    protected abstract string GetScriptExtension(ScriptRuntime runtime);

    /// <summary>Ajustes del archivo previos a ejecutarlo, como los permisos en Linux.</summary>
    protected virtual void PrepareScriptFile(string scriptPath)
    {
    }

    private async Task<ScriptExecutionOutcome> RunAsync(ExecuteScriptMessage request, string scriptPath, Func<int, Task> onStarted, Func<OutputStream, string, Task> onOutput, CancellationToken cancellationToken)
    {
        var (fileName, arguments) = BuildCommand(request.Runtime, scriptPath);

        using var process = new Process { StartInfo = BuildStartInfo(request, fileName, arguments, scriptPath), EnableRaisingEvents = true };

        if (!process.Start())
        {
            return ScriptExecutionOutcome.Rejected($"No se pudo arrancar {fileName}.");
        }

        await onStarted(process.Id);

        if (request.Mode is ExecutionMode.Detached)
        {
            // Fire and forget: el proceso sigue vivo por su cuenta y el agente no observa su salida.
            logger.LogInformation("Ejecución {ExecutionId} lanzada en modo Detached con PID {ProcessId}", request.ExecutionId, process.Id);
            return ScriptExecutionOutcome.Launched();
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(request.TimeoutSeconds, 1)));

        var stdout = PumpAsync(process.StandardOutput, OutputStream.Stdout, onOutput, timeout.Token);
        var stderr = PumpAsync(process.StandardError, OutputStream.Stderr, onOutput, timeout.Token);

        try
        {
            await process.WaitForExitAsync(timeout.Token);

            // Después de WaitForExit las bombas terminan solas al cerrarse las tuberías.
            await Task.WhenAll(stdout, stderr);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            KillTree(process, request.ExecutionId);
            return ScriptExecutionOutcome.TimedOut(request.TimeoutSeconds);
        }

        return process.ExitCode == 0 ? ScriptExecutionOutcome.Succeeded(process.ExitCode) : ScriptExecutionOutcome.Failed(process.ExitCode);
    }

    /// <summary>
    /// Lee el flujo en bloques en lugar de por líneas: una única línea muy larga no puede hacer crecer
    /// la memoria del agente sin límite.
    /// </summary>
    private async Task PumpAsync(StreamReader reader, OutputStream stream, Func<OutputStream, string, Task> onOutput, CancellationToken cancellationToken)
    {
        var buffer = new char[Math.Max(options.Value.OutputChunkChars, 256)];

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);

            if (read == 0)
            {
                return;
            }

            await onOutput(stream, new string(buffer, 0, read));
        }
    }

    private void KillTree(Process process, Guid executionId)
    {
        try
        {
            // El árbol entero: un script que lanzó procesos hijos no puede dejarlos huérfanos al vencer.
            process.Kill(entireProcessTree: true);
            logger.LogWarning("Ejecución {ExecutionId} terminada por tiempo junto con su árbol de procesos", executionId);
        }
        catch (Exception exception)
        {
            logger.LogWarning("No se pudo terminar el árbol de la ejecución {ExecutionId}: {Motivo}", executionId, exception.Message);
        }
    }

    private ProcessStartInfo BuildStartInfo(ExecuteScriptMessage request, string fileName, string arguments, string scriptPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = ResolveWorkingDirectory(request, scriptPath),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var variable in request.EnvironmentVariables ?? EmptyEnvironment)
        {
            startInfo.Environment[variable.Key] = variable.Value;
        }

        return startInfo;
    }

    private static string ResolveWorkingDirectory(ExecuteScriptMessage request, string scriptPath)
    {
        if (!string.IsNullOrWhiteSpace(request.WorkingDirectory) && Directory.Exists(request.WorkingDirectory))
        {
            return request.WorkingDirectory;
        }

        return Path.GetDirectoryName(scriptPath)!;
    }

    private async Task<string> WriteScriptAsync(string directory, ExecuteScriptMessage request, CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(directory, $"script{GetScriptExtension(request.Runtime)}");

        await File.WriteAllTextAsync(scriptPath, request.ScriptContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
        PrepareScriptFile(scriptPath);

        return scriptPath;
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyEnvironment = new Dictionary<string, string>();

    private static bool ChecksumMatches(ExecuteScriptMessage request) => string.Equals(ComputeChecksum(request.ScriptContent), request.ScriptChecksum, StringComparison.OrdinalIgnoreCase);

    /// <summary>SHA-256 del contenido en hexadecimal, calculado igual que en el orquestador.</summary>
    public static string ComputeChecksum(string content) => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}
