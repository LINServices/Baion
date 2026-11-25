using System;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ServiceLogsRequestMessage(Guid RequestId, string ServiceId, int MaxLines, DateTimeOffset? Since) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "service-logs-request";
}
