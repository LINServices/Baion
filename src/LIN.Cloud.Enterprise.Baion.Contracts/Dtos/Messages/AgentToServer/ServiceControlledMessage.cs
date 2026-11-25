using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Services;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ServiceControlledMessage(Guid RequestId, ServiceDetail Service) : AgentToServerMessage, IAgentQueryReply
{
    public const string TypeDiscriminator = "service-controlled";
}
