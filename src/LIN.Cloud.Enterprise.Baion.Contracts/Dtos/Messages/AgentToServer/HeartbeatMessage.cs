namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record HeartbeatMessage(int RunningExecutions) : AgentToServerMessage
{
    public const string TypeDiscriminator = "heartbeat";
}
