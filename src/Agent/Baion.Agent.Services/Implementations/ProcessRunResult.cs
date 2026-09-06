namespace Baion.Agent.Services.Implementations;

/// <summary>Resultado de invocar una herramienta del sistema una sola vez.</summary>
internal record ProcessRunResult(int? ExitCode, string StandardOutput, string StandardError, bool TimedOut, string? LaunchError)
{
    /// <summary>El proceso llegó a arrancar (independientemente de su código de salida).</summary>
    public bool Launched => LaunchError is null;

    /// <summary>El proceso arrancó, no se pasó de tiempo y devolvió 0.</summary>
    public bool Succeeded => LaunchError is null && !TimedOut && ExitCode == 0;

    public static ProcessRunResult NotStarted(string detail) => new(null, string.Empty, string.Empty, false, detail);

    public static ProcessRunResult FromTimeout() => new(null, string.Empty, string.Empty, true, null);

    public static ProcessRunResult Completed(int exitCode, string standardOutput, string standardError) => new(exitCode, standardOutput, standardError, false, null);
}
