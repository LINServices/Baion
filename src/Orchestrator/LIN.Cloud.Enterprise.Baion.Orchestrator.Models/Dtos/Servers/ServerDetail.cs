namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ServerDetail(ServerSummary Server, ServerMetricsSnapshot? LastMetrics);
