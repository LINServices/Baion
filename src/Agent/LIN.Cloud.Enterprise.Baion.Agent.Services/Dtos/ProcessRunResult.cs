namespace LIN.Cloud.Enterprise.Baion.Agent.Services.Implementations;

// Resultado de invocar una herramienta del sistema una sola vez.
internal record ProcessRunResult(int? ExitCode, string StandardOutput, string StandardError, bool TimedOut, string? LaunchError)
{
    // El proceso llegó a arrancar (independientemente de su código de salida).
    public bool Launched => LaunchError is null;

    // El proceso arrancó, no se pasó de tiempo y devolvió 0.
    public bool Succeeded => LaunchError is null && !TimedOut && ExitCode == 0;

    public static ProcessRunResult NotStarted(string detail)
    {
        return new ProcessRunResult(null, string.Empty, string.Empty, false, detail);
    }

    public static ProcessRunResult FromTimeout()
    {
        return new ProcessRunResult(null, string.Empty, string.Empty, true, null);
    }

    public static ProcessRunResult Completed(int exitCode, string standardOutput, string standardError)
    {
        return new ProcessRunResult(exitCode, standardOutput, standardError, false, null);
    }
}
