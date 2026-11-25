using System;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record WelcomeMessage(Guid ServerId, Guid TenantId, int HeartbeatSeconds, int MaxConcurrentExecutions) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "welcome";

    // Solo viaja en el enrolamiento inicial; el agente la persiste como credencial permanente
    public string? AgentToken { get; init; }
}
