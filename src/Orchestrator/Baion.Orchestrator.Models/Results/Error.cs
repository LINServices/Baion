namespace Baion.Orchestrator.Models.Results;

/// <summary>Fallo esperado de una operación: qué tipo de fallo es, un código estable y un mensaje legible.</summary>
public record Error(ErrorKind Kind, string Code, string Message)
{
    public static Error Validation(string code, string message) => new(ErrorKind.Validation, code, message);

    public static Error NotFound(string code, string message) => new(ErrorKind.NotFound, code, message);

    public static Error Conflict(string code, string message) => new(ErrorKind.Conflict, code, message);

    public static Error Unauthorized(string code, string message) => new(ErrorKind.Unauthorized, code, message);

    public static Error Forbidden(string code, string message) => new(ErrorKind.Forbidden, code, message);

    public static Error Unexpected(string code, string message) => new(ErrorKind.Unexpected, code, message);
}
