namespace Baion.Orchestrator.Models.Results;

/// <summary>Resultado de una operación sin valor de retorno. Las excepciones quedan para lo verdaderamente inesperado.</summary>
public class Result
{
    public bool IsSuccess { get; private init; }

    public Error? Error { get; private init; }

    public bool IsFailure => !IsSuccess;

    public static Result Success() => new() { IsSuccess = true };

    public static Result Failure(Error error) => new() { IsSuccess = false, Error = error };
}
