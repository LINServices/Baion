using System;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ServiceLogStreamStopMessage(Guid SubscriptionId) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "service-logs-stream-stop";
}
