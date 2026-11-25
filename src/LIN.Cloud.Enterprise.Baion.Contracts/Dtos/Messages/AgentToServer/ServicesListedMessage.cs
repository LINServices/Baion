using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Services;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ServicesListedMessage(Guid RequestId, IReadOnlyList<ServiceSummary> Services) : AgentToServerMessage, IAgentQueryReply
{
    public const string TypeDiscriminator = "services-listed";
}
