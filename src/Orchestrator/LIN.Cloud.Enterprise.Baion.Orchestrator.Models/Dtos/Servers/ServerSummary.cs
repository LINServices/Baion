using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ServerSummary(Guid Id, string Name, string Hostname, ServerKind Kind, ServerPlatform Platform, ServerStatus Status, string? AgentVersion, string? RuntimeIdentifier, string? OrchestratorInstanceId, DateTimeOffset? ConnectedAt, DateTimeOffset? LastSeenAt, int MaxConcurrentExecutions);
