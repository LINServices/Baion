using System;

namespace Baion.Contracts.Messages;

/// <summary>Pide al agente el detalle de un servicio concreto.</summary>
public record DescribeServiceRequestMessage(Guid RequestId, string ServiceId) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "service-describe-request";
}
