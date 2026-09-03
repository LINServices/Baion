using System;

namespace Baion.Agent.Core;

/// <summary>Parámetros que el orquestador fijó para la sesión en curso.</summary>
public record AgentSessionInfo(Guid ServerId, Guid TenantId, int MaxConcurrentExecutions);
