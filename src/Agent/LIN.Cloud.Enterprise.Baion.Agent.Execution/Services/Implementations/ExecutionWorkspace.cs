using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace LIN.Cloud.Enterprise.Baion.Agent.Execution.Implementations;

// Carpetas donde se materializan los scripts antes de ejecutarlos.
internal static class ExecutionWorkspace
{
    public static string ResolveRoot(ScriptExecutionOptions options)
    {
        return string.IsNullOrWhiteSpace(options.WorkingRoot) ? Path.Combine(Path.GetTempPath(), "baion-agent") : options.WorkingRoot;
    }

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

    // Las carpetas de ejecuciones Detached se conservan a propósito mientras su proceso vive,
    // así que la única forma de recogerlas es por antigüedad al arrancar.
    public static void Sweep(ScriptExecutionOptions options, TimeSpan maxAge, TimeProvider timeProvider, ILogger logger)
    {
        var root = ResolveRoot(options);

        if (!Directory.Exists(root))
            return;

        var limite = timeProvider.GetUtcNow() - maxAge;
        var antiguas = Directory.EnumerateDirectories(root)
            .Where(directory => Directory.GetLastWriteTimeUtc(directory) < limite)
            .ToList();

        foreach (var directory in antiguas)
            TryDelete(directory, logger);

        if (antiguas.Count > 0)
            logger.LogInformation("Recogidas {Carpetas} carpetas de ejecuciones antiguas", antiguas.Count);
    }
}
