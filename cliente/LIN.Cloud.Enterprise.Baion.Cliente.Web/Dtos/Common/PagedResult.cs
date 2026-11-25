using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Common;

public record PagedResult<TItem>(IReadOnlyList<TItem> Items, int Page, int PageSize, int TotalCount, int TotalPages, bool HasPrevious, bool HasNext);
