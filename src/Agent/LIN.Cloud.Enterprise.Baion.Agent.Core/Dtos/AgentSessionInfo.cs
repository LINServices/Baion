using System;

namespace LIN.Cloud.Enterprise.Baion.Agent.Core;

public record AgentSessionInfo(Guid ServerId, Guid TenantId, int MaxConcurrentExecutions);
