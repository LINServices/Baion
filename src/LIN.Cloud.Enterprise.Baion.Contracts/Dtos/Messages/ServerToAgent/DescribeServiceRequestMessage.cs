using System;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record DescribeServiceRequestMessage(Guid RequestId, string ServiceId) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "service-describe-request";
}
