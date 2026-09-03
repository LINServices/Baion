using Baion.Contracts.Messages;

namespace Baion.Agent.Execution;

/// <summary>
/// Admite las órdenes de ejecución y las lleva en paralelo hasta el tope de concurrencia que fijó el
/// orquestador. Admitir nunca bloquea: el bucle de recepción del socket no puede quedarse esperando un hueco.
/// </summary>
public interface IScriptExecutionCoordinator
{
    /// <summary>Acepta la orden y la ejecuta en segundo plano.</summary>
    void Enqueue(ExecuteScriptMessage request);

    /// <summary>Ejecuciones en curso ahora mismo.</summary>
    int RunningCount { get; }
}
