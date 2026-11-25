using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Services;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ServiceDescribedMessage(Guid RequestId, ServiceDetail Service) : AgentToServerMessage, IAgentQueryReply
{
    public const string TypeDiscriminator = "service-described";
}
