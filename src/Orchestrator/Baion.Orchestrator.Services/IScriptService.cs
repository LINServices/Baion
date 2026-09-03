using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Services;

/// <summary>Alta y consulta de los scripts del tenant actual.</summary>
public interface IScriptService
{
    /// <summary>Da de alta un script y calcula su checksum a partir del contenido.</summary>
    Task<Result<ScriptSummary>> CreateAsync(CreateScriptRequest request, CancellationToken cancellationToken);

    /// <summary>Edita un script del tenant. La versión sube solo cuando cambia el contenido.</summary>
    Task<Result<ScriptSummary>> UpdateAsync(Guid scriptId, UpdateScriptRequest request, CancellationToken cancellationToken);

    /// <summary>Obtiene la ficha de un script.</summary>
    Task<Result<ScriptSummary>> GetAsync(Guid scriptId);

    /// <summary>Lista los scripts del tenant, filtrando por nombre cuando se indica una búsqueda.</summary>
    Task<PagedResult<ScriptListItem>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Obtiene un script con su contenido.</summary>
    Task<Result<ScriptDetail>> GetDetailAsync(Guid scriptId, CancellationToken cancellationToken);
}
