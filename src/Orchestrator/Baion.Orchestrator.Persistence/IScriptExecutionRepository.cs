using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Orchestrator.Models.Entities;

namespace Baion.Orchestrator.Persistence;

/// <summary>Acceso a las ejecuciones de script del tenant actual.</summary>
public interface IScriptExecutionRepository
{
    /// <summary>Obtiene una ejecución del tenant actual.</summary>
    Task<ScriptExecution?> GetByIdAsync(Guid executionId, CancellationToken cancellationToken);

    /// <summary>Obtiene una ejecución con su servidor y su script cargados, para mostrarla con nombres.</summary>
    Task<ScriptExecution?> GetWithNamesAsync(Guid executionId, CancellationToken cancellationToken);

    /// <summary>
    /// Ejecuciones de cualquier tenant que siguen esperando entrega. El barrido de reintentos es de
    /// instancia, así que ignora el filtro global y devuelve el tenant de cada fila.
    /// </summary>
    Task<IReadOnlyList<ScriptExecution>> GetPendingDispatchesAsync(int limit, CancellationToken cancellationToken);

    /// <summary>Obtiene todas las ejecuciones de un recorrido de cadena, ordenadas por el paso al que pertenecen.</summary>
    Task<IReadOnlyList<ScriptExecution>> GetByChainRunAsync(Guid chainRunId, CancellationToken cancellationToken);

    /// <summary>
    /// Añade texto al final de la salida acumulada. Usa <c>.WRITE</c> de SQL Server para no releer ni
    /// reescribir lo ya guardado: con salidas de megabytes, concatenar sería cuadrático.
    /// </summary>
    Task AppendOutputAsync(Guid executionId, OutputStream stream, string content, CancellationToken cancellationToken);
}
