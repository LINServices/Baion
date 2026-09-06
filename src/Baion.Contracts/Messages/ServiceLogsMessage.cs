using System;
using System.Collections.Generic;
using Baion.Contracts.Services;

namespace Baion.Contracts.Messages;

/// <summary>Respuesta a <see cref="ServiceLogsRequestMessage"/> con la instantánea de logs.</summary>
/// <param name="Truncated">El agente recortó el resultado para no pasarse del tamaño máximo de trama.</param>
public record ServiceLogsMessage(Guid RequestId, IReadOnlyList<ServiceLogLine> Lines, bool Truncated) : AgentToServerMessage, IAgentQueryReply
{
    public const string TypeDiscriminator = "service-logs";
}
