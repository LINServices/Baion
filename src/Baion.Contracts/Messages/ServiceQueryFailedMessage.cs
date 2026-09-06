using System;

namespace Baion.Contracts.Messages;

/// <summary>
/// El agente no pudo atender una petición de servicios: el servicio no existe, faltan permisos, la
/// herramienta del sistema falló, etc. Cierra la petición correlacionada con un error.
/// </summary>
public record ServiceQueryFailedMessage(Guid RequestId, string Code, string Message) : AgentToServerMessage, IAgentQueryReply
{
    public const string TypeDiscriminator = "service-query-failed";

    /// <summary>El servicio indicado no existe en la máquina.</summary>
    public const string NotFoundCode = "service.not_found";

    /// <summary>El agente no tiene permisos para la operación pedida.</summary>
    public const string ForbiddenCode = "service.forbidden";

    /// <summary>La herramienta del sistema devolvió un error al ejecutar la operación.</summary>
    public const string ToolFailedCode = "service.tool_failed";
}
