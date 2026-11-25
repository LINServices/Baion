namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public static class Pagination
{
    public const int FirstPage = 1;

    public const int DefaultPageSize = 25;

    // Tope duro: sin él una sola petición podría arrastrar la tabla entera.
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var normalizedPage = page < FirstPage ? FirstPage : page;
        var normalizedPageSize = pageSize switch
        {
            <= 0 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize
        };

        return (normalizedPage, normalizedPageSize);
    }
}
