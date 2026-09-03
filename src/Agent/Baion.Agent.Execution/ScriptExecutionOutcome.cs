using Baion.Contracts.Enums;

namespace Baion.Agent.Execution;

/// <summary>Desenlace de ejecutar un script en la máquina.</summary>
public record ScriptExecutionOutcome(ExecutionStatus Status, int? ExitCode, string? ErrorMessage)
{
    public static ScriptExecutionOutcome Succeeded(int exitCode) => new(ExecutionStatus.Succeeded, exitCode, null);

    public static ScriptExecutionOutcome Failed(int exitCode) => new(ExecutionStatus.Failed, exitCode, null);

    public static ScriptExecutionOutcome TimedOut(int timeoutSeconds) => new(ExecutionStatus.TimedOut, null, $"La ejecución superó el límite de {timeoutSeconds} s y se terminó junto con sus procesos hijos.");

    public static ScriptExecutionOutcome Rejected(string reason) => new(ExecutionStatus.Failed, null, reason);

    /// <summary>Lanzamiento correcto en modo Detached: el agente no llega a observar el código de salida.</summary>
    public static ScriptExecutionOutcome Launched() => new(ExecutionStatus.Succeeded, null, null);
}
