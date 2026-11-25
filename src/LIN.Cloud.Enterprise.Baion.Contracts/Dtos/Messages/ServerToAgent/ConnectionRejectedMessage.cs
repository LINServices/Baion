namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ConnectionRejectedMessage(string Code, string Reason) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "connection-rejected";
}
