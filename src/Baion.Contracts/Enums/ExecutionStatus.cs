namespace Baion.Contracts.Enums;

/// <summary>Estado del ciclo de vida de una ejecución de script.</summary>
public enum ExecutionStatus
{
    Pending = 1,
    Dispatched = 2,
    Running = 3,
    Succeeded = 4,
    Failed = 5,
    TimedOut = 6,
    Canceled = 7
}
