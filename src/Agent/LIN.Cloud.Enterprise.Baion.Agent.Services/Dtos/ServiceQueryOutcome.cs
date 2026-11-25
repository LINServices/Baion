using LIN.Cloud.Enterprise.Baion.Contracts.Messages;

namespace LIN.Cloud.Enterprise.Baion.Agent.Services;

// Desenlace de una consulta de servicios: o bien un valor, o bien uno de los códigos de
// ServiceQueryFailedMessage con su mensaje. Se traduce a mensaje de respuesta en el procesador.
public record ServiceQueryOutcome<T>(T? Value, string? FailureCode, string? FailureMessage)
{
    public bool IsSuccess => FailureCode is null;

    public static ServiceQueryOutcome<T> Ok(T value)
    {
        return new ServiceQueryOutcome<T>(value, null, null);
    }

    public static ServiceQueryOutcome<T> NotFound(string message)
    {
        return new ServiceQueryOutcome<T>(default, ServiceQueryFailedMessage.NotFoundCode, message);
    }

    public static ServiceQueryOutcome<T> Forbidden(string message)
    {
        return new ServiceQueryOutcome<T>(default, ServiceQueryFailedMessage.ForbiddenCode, message);
    }

    public static ServiceQueryOutcome<T> ToolFailed(string message)
    {
        return new ServiceQueryOutcome<T>(default, ServiceQueryFailedMessage.ToolFailedCode, message);
    }
}
