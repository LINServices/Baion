using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;

namespace Baion.Orchestrator.Persistence;

/// <summary>
/// Consultas de solo lectura sobre scripts y ejecuciones. Van aparte de los repositorios porque proyectan
/// directamente a DTO: traer entidades enteras para pintar una tabla es trabajo de más.
/// </summary>
public interface IScriptQueries
{
    /// <summary>Página de scripts del tenant, filtrando por nombre cuando <paramref name="search"/> trae algo.</summary>
    Task<PagedResult<ScriptListItem>> ListScriptsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Ficha completa de un script con su contenido, o null si no existe en el tenant.</summary>
    Task<ScriptDetail?> GetScriptDetailAsync(Guid scriptId, CancellationToken cancellationToken);

    /// <summary>Página de ejecuciones que cumplen el filtro, de la más reciente a la más antigua.</summary>
    Task<PagedResult<ScriptExecutionListItem>> ListExecutionsAsync(ExecutionFilter filter, int page, int pageSize, CancellationToken cancellationToken);
}
