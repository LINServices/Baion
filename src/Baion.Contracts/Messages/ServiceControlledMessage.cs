using System;
using Baion.Contracts.Services;

namespace Baion.Contracts.Messages;

/// <summary>Respuesta a <see cref="ControlServiceRequestMessage"/> con el detalle del servicio tras la acción.</summary>
public record ServiceControlledMessage(Guid RequestId, ServiceDetail Service) : AgentToServerMessage, IAgentQueryReply
{
    public const string TypeDiscriminator = "service-controlled";
}
