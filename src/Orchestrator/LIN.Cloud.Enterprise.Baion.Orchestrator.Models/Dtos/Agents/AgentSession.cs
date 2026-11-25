using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record AgentSession(Guid TenantId, Guid ServerId, int MaxConcurrentExecutions, int HeartbeatSeconds)
{
    // Credencial permanente recién emitida. Solo tiene valor en el enrolamiento inicial.
    public string? IssuedAgentToken { get; init; }
}
