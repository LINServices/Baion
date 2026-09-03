namespace Baion.Orchestrator.Models.Results;

/// <summary>Naturaleza de un fallo esperado, para que la capa web lo traduzca a un código HTTP.</summary>
public enum ErrorKind
{
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    Unexpected = 6
}
