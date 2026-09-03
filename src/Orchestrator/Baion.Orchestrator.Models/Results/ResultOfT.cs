namespace Baion.Orchestrator.Models.Results;

/// <summary>Resultado de una operación que devuelve un valor cuando tiene éxito.</summary>
public class Result<TValue>
{
    public bool IsSuccess { get; private init; }

    public TValue? Value { get; private init; }

    public Error? Error { get; private init; }

    public bool IsFailure => !IsSuccess;

    public static Result<TValue> Success(TValue value) => new() { IsSuccess = true, Value = value };

    public static Result<TValue> Failure(Error error) => new() { IsSuccess = false, Error = error };
}
