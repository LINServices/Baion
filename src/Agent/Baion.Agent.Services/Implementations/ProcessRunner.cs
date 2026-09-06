using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Baion.Agent.Services.Implementations;

/// <summary>
/// Lanza una herramienta del sistema, recoge toda su salida y la corta junto con su árbol de procesos si
/// se pasa del tiempo. Los argumentos van por <see cref="ProcessStartInfo.ArgumentList"/>, nunca por una
/// shell: el identificador del servicio llega del orquestador y no se interpola en ninguna línea de comando.
/// </summary>
internal static class ProcessRunner
{
    public static async Task<ProcessRunResult> RunAsync(string fileName, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                return ProcessRunResult.NotStarted($"No se pudo arrancar {fileName}.");
            }
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            return ProcessRunResult.NotStarted($"No se pudo arrancar {fileName}: {exception.Message}");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            KillQuietly(process);

            // Las bombas de salida terminan solas al cerrarse las tuberías; se drenan sin propagar su error.
            await Task.WhenAll(Swallow(stdoutTask), Swallow(stderrTask));
            return ProcessRunResult.FromTimeout();
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return ProcessRunResult.Completed(process.ExitCode, stdout, stderr);
    }

    private static void KillQuietly(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            // El proceso ya estaba terminando; nada que hacer.
        }
    }

    private static async Task Swallow(Task<string> task)
    {
        try
        {
            await task;
        }
        catch (Exception)
        {
            // La salida ya no interesa: el proceso se terminó por tiempo.
        }
    }
}
