using System;
using System.Collections.Generic;
using Baion.Contracts.Services;

namespace Baion.Contracts.Messages;

/// <summary>Respuesta a <see cref="ListServicesRequestMessage"/> con el listado de servicios.</summary>
public record ServicesListedMessage(Guid RequestId, IReadOnlyList<ServiceSummary> Services) : AgentToServerMessage, IAgentQueryReply
{
    public const string TypeDiscriminator = "services-listed";
}
