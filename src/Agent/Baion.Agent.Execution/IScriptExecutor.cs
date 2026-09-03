using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Contracts.Messages;

namespace Baion.Agent.Execution;

/// <summary>Ejecuta un script en la máquina del agente. Hay una implementación por plataforma.</summary>
public interface IScriptExecutor
{
    /// <summary>Plataforma que atiende esta implementación.</summary>
    ServerPlatform Platform { get; }

    /// <summary>
    /// Lanza el script y devuelve su desenlace. Los fragmentos de salida se entregan a
    /// <paramref name="onOutput"/> según van saliendo del proceso, sin acumularlos en memoria.
    /// </summary>
    /// <param name="onStarted">Se invoca con el identificador del proceso en cuanto arranca.</param>
    Task<ScriptExecutionOutcome> ExecuteAsync(ExecuteScriptMessage request, Func<int, Task> onStarted, Func<OutputStream, string, Task> onOutput, CancellationToken cancellationToken);
}
