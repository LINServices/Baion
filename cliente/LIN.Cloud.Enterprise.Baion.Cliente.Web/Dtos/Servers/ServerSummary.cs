using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Servers;

public record ServerSummary(Guid Id, string Name, string Hostname, string Kind, string Platform, string Status, string? AgentVersion, string? RuntimeIdentifier, string? OrchestratorInstanceId, DateTimeOffset? ConnectedAt, DateTimeOffset? LastSeenAt, int MaxConcurrentExecutions);
