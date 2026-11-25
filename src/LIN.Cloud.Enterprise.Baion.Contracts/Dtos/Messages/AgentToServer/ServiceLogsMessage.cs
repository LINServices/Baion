using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Services;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ServiceLogsMessage(Guid RequestId, IReadOnlyList<ServiceLogLine> Lines, bool Truncated) : AgentToServerMessage, IAgentQueryReply
{
    public const string TypeDiscriminator = "service-logs";
}
