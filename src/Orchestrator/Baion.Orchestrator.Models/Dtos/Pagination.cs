namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Límites de paginación comunes a todos los listados de la API.</summary>
public static class Pagination
{
    /// <summary>Encaja lo que llegue de la petición dentro de los límites admitidos.</summary>
    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var pagina = page < FirstPage ? FirstPage : page;
        var tamano = pageSize switch
        {
            <= 0 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize
        };

        return (pagina, tamano);
    }

    public const int FirstPage = 1;

    public const int DefaultPageSize = 25;

    /// <summary>Tope duro: sin él una sola petición podría arrastrar la tabla entera.</summary>
    public const int MaxPageSize = 100;
}
