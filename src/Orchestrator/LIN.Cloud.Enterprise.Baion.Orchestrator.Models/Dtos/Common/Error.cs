namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Results;

public record Error(ErrorKind Kind, string Code, string Message)
{
    public static Error Validation(string code, string message)
    {
        return new(ErrorKind.Validation, code, message);
    }

    public static Error NotFound(string code, string message)
    {
        return new(ErrorKind.NotFound, code, message);
    }

    public static Error Conflict(string code, string message)
    {
        return new(ErrorKind.Conflict, code, message);
    }

    public static Error Unauthorized(string code, string message)
    {
        return new(ErrorKind.Unauthorized, code, message);
    }

    public static Error Forbidden(string code, string message)
    {
        return new(ErrorKind.Forbidden, code, message);
    }

    public static Error Unexpected(string code, string message)
    {
        return new(ErrorKind.Unexpected, code, message);
    }
}
