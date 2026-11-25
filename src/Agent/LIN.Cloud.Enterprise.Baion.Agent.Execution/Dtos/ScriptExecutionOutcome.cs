using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Agent.Execution;

public record ScriptExecutionOutcome(ExecutionStatus Status, int? ExitCode, string? ErrorMessage)
{
    public static ScriptExecutionOutcome Succeeded(int exitCode)
    {
        return new ScriptExecutionOutcome(ExecutionStatus.Succeeded, exitCode, null);
    }

    public static ScriptExecutionOutcome Failed(int exitCode)
    {
        return new ScriptExecutionOutcome(ExecutionStatus.Failed, exitCode, null);
    }

    public static ScriptExecutionOutcome TimedOut(int timeoutSeconds)
    {
        return new ScriptExecutionOutcome(ExecutionStatus.TimedOut, null, $"La ejecución superó el límite de {timeoutSeconds} s y se terminó junto con sus procesos hijos.");
    }

    public static ScriptExecutionOutcome Rejected(string reason)
    {
        return new ScriptExecutionOutcome(ExecutionStatus.Failed, null, reason);
    }

    // Lanzamiento correcto en modo Detached: el agente no llega a observar el código de salida.
    public static ScriptExecutionOutcome Launched()
    {
        return new ScriptExecutionOutcome(ExecutionStatus.Succeeded, null, null);
    }
}
