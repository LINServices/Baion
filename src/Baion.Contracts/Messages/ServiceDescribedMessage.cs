using System;
using Baion.Contracts.Services;

namespace Baion.Contracts.Messages;

/// <summary>Respuesta a <see cref="DescribeServiceRequestMessage"/> con el detalle del servicio.</summary>
public record ServiceDescribedMessage(Guid RequestId, ServiceDetail Service) : AgentToServerMessage, IAgentQueryReply
{
    public const string TypeDiscriminator = "service-described";
}
