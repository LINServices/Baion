using System.Collections.Generic;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Página de un listado, con lo justo para que el cliente pinte un paginador.</summary>
public record PagedResult<TItem>(IReadOnlyList<TItem> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>Nunca baja de 1: un listado vacío sigue teniendo una página que mostrar.</summary>
    public int TotalPages => TotalCount <= 0 || PageSize <= 0 ? 1 : ((TotalCount - 1) / PageSize) + 1;

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}
