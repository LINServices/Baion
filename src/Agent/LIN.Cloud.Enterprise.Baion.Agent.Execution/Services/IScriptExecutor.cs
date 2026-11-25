using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Contracts.Messages;

namespace LIN.Cloud.Enterprise.Baion.Agent.Execution;

/// <summary>
/// Ejecuta un script en la máquina del agente. Hay una implementación por plataforma.
/// </summary>
public interface IScriptExecutor
{
    /// <summary>
    /// Plataforma que atiende esta implementación.
    /// </summary>
    ServerPlatform Platform { get; }

    /// <summary>
    /// Lanza el script y devuelve su desenlace. Los fragmentos de salida se entregan a
    /// <paramref name="onOutput"/> según van saliendo del proceso, sin acumularlos en memoria.
    /// </summary>
    /// <param name="request">Orden de ejecución recibida del orquestador.</param>
    /// <param name="onStarted">Se invoca con el identificador del proceso en cuanto arranca.</param>
    /// <param name="onOutput">Recibe cada fragmento de salida con el flujo del que viene.</param>
    /// <param name="cancellationToken">Cancela la ejecución en curso.</param>
    Task<ScriptExecutionOutcome> ExecuteAsync(ExecuteScriptMessage request, Func<int, Task> onStarted, Func<OutputStream, string, Task> onOutput, CancellationToken cancellationToken);
}
