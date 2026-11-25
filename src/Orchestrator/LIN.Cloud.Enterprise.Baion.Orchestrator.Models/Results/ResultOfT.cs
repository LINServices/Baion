namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Results;

public class Result<TValue>
{
    public bool IsSuccess { get; private init; }

    public TValue? Value { get; private init; }

    public Error? Error { get; private init; }

    public bool IsFailure => !IsSuccess;

    public static Result<TValue> Success(TValue value)
    {
        return new() { IsSuccess = true, Value = value };
    }

    public static Result<TValue> Failure(Error error)
    {
        return new() { IsSuccess = false, Error = error };
    }
}
