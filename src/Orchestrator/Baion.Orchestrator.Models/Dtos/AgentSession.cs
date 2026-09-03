using System;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Sesión establecida tras un handshake correcto.</summary>
public record AgentSession(Guid TenantId, Guid ServerId, int MaxConcurrentExecutions, int HeartbeatSeconds)
{
    /// <summary>Credencial permanente recién emitida. Solo tiene valor en el enrolamiento inicial.</summary>
    public string? IssuedAgentToken { get; init; }
}
