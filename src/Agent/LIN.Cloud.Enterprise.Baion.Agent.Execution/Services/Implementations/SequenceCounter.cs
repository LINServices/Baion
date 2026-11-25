namespace LIN.Cloud.Enterprise.Baion.Agent.Execution.Implementations;

// Contador por ejecución; Interlocked necesita una referencia estable sobre la que operar.
internal sealed class SequenceCounter
{
    public long Value;
}
