using System;

namespace Baion.Contracts.Messages;

/// <summary>Respuesta del orquestador al saludo: confirma el registro y fija los parámetros de la sesión.</summary>
public record WelcomeMessage(Guid ServerId, Guid TenantId, int HeartbeatSeconds, int MaxConcurrentExecutions) : ServerToAgentMessage
{
    /// <summary>Credencial permanente del agente. Solo viaja en el enrolamiento inicial; el agente la persiste.</summary>
    public string? AgentToken { get; init; }

    public const string TypeDiscriminator = "welcome";
}
