namespace Baion.Cliente.Web.Services;

/// <summary>
/// Desenlace de una llamada a la API. El panel no lanza excepciones por un 401 o un 404: los pinta.
/// </summary>
public class ApiResult<TValue>
{
    public bool IsSuccess { get; private init; }

    public TValue? Value { get; private init; }

    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Código del error tal y como lo nombra la API en el <c>title</c> del <c>ProblemDetails</c>
    /// (p. ej. <c>script.name_required</c>), para que quien llame lo mapee a un mensaje de campo.
    /// </summary>
    public string? ErrorCode { get; private init; }

    public bool IsFailure => !IsSuccess;

    public static ApiResult<TValue> Success(TValue value) => new() { IsSuccess = true, Value = value };

    public static ApiResult<TValue> Failure(string errorMessage, string? errorCode = null) => new() { IsSuccess = false, ErrorMessage = errorMessage, ErrorCode = errorCode };
}
