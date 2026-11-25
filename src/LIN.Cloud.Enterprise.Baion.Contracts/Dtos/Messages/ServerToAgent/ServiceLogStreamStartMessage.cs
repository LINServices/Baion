using System;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ServiceLogStreamStartMessage(Guid SubscriptionId, string ServiceId) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "service-logs-stream-start";
}
