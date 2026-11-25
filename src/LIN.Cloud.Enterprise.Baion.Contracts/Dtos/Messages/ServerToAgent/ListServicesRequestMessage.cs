using System;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ListServicesRequestMessage(Guid RequestId, string? NameFilter) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "services-list-request";
}
