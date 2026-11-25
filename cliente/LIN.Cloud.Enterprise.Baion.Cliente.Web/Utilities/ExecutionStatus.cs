namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Utilities;

public static class ExecutionStatus
{
    public static bool IsLive(string status)
    {
        return status is "pending" or "dispatched" or "running";
    }
}
