using Baion.Contracts.Messages;

namespace Baion.Agent.Services;

/// <summary>
/// Desenlace de una consulta de servicios: o bien un valor, o bien uno de los códigos de
/// <see cref="ServiceQueryFailedMessage"/> con su mensaje. Se traduce a mensaje de respuesta en el procesador.
/// </summary>
public record ServiceQueryOutcome<T>(T? Value, string? FailureCode, string? FailureMessage)
{
    /// <summary>La consulta se resolvió con un valor.</summary>
    public bool IsSuccess => FailureCode is null;

    public static ServiceQueryOutcome<T> Ok(T value) => new(value, null, null);

    public static ServiceQueryOutcome<T> NotFound(string message) => new(default, ServiceQueryFailedMessage.NotFoundCode, message);

    public static ServiceQueryOutcome<T> Forbidden(string message) => new(default, ServiceQueryFailedMessage.ForbiddenCode, message);

    public static ServiceQueryOutcome<T> ToolFailed(string message) => new(default, ServiceQueryFailedMessage.ToolFailedCode, message);
}
