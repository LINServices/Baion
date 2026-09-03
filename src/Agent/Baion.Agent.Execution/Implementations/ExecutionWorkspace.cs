using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Baion.Agent.Execution.Implementations;

/// <summary>Carpetas donde se materializan los scripts antes de ejecutarlos.</summary>
internal static class ExecutionWorkspace
{
    public static string ResolveRoot(ScriptExecutionOptions options) => string.IsNullOrWhiteSpace(options.WorkingRoot) ? Path.Combine(Path.GetTempPath(), "baion-agent") : options.WorkingRoot;

    public static string CreateFor(ScriptExecutionOptions options, Guid executionId)
    {
        var directory = Path.Combine(ResolveRoot(options), executionId.ToString("N"));
        Directory.CreateDirectory(directory);

        return directory;
    }

    public static void TryDelete(string directory, ILogger logger)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug("No se pudo borrar {Directorio}: {Motivo}", directory, exception.Message);
        }
    }

    /// <summary>
    /// Barre las carpetas que quedaron atrás. Las de ejecuciones Detached se conservan a propósito
    /// mientras su proceso vive, así que la única forma de recogerlas es por antigüedad al arrancar.
    /// </summary>
    public static void Sweep(ScriptExecutionOptions options, TimeSpan maxAge, TimeProvider timeProvider, ILogger logger)
    {
        var root = ResolveRoot(options);

        if (!Directory.Exists(root))
        {
            return;
        }

        var limite = timeProvider.GetUtcNow() - maxAge;
        var borradas = 0;

        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            if (Directory.GetLastWriteTimeUtc(directory) >= limite)
            {
                continue;
            }

            TryDelete(directory, logger);
            borradas++;
        }

        if (borradas > 0)
        {
            logger.LogInformation("Recogidas {Carpetas} carpetas de ejecuciones antiguas", borradas);
        }
    }
}
