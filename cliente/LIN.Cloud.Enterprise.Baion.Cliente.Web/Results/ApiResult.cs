namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Results;

public class ApiResult<TValue>
{
    public bool IsSuccess { get; private init; }

    public TValue? Value { get; private init; }

    public string? ErrorMessage { get; private init; }

    public string? ErrorCode { get; private init; }

    public bool IsFailure => !IsSuccess;

    public static ApiResult<TValue> Success(TValue value)
    {
        return new() { IsSuccess = true, Value = value };
    }

    public static ApiResult<TValue> Failure(string errorMessage, string? errorCode = null)
    {
        return new() { IsSuccess = false, ErrorMessage = errorMessage, ErrorCode = errorCode };
    }
}
