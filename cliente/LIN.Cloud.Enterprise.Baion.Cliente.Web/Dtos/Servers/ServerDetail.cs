namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Servers;

public record ServerDetail(ServerSummary Server, ServerMetricsSnapshot? LastMetrics);
