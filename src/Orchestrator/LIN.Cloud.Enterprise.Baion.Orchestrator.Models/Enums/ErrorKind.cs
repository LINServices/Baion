namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Results;

// Naturaleza de un fallo esperado, para que la capa web lo traduzca a un código HTTP.
public enum ErrorKind
{
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    Unexpected = 6
}
