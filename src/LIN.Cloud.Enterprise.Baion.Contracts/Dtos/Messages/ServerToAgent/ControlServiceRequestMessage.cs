using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ControlServiceRequestMessage(Guid RequestId, string ServiceId, ServiceControlAction Action) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "service-control-request";
}
