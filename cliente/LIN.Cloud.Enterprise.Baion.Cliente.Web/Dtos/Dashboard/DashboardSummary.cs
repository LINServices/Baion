namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Dashboard;

public record DashboardSummary(int TotalServers, int OnlineServers, int OfflineServers, int TotalScripts, int RunningExecutions, int ExecutionsLast24Hours, int FailedLast24Hours);
