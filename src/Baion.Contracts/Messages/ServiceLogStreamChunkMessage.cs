using System;
using System.Collections.Generic;
using Baion.Contracts.Services;

namespace Baion.Contracts.Messages;

/// <summary>Lote de líneas nuevas de un seguimiento de logs en vivo.</summary>
/// <remarks>Contrato reservado para una fase posterior; el agente todavía no lo emite.</remarks>
public record ServiceLogStreamChunkMessage(Guid SubscriptionId, IReadOnlyList<ServiceLogLine> Lines) : AgentToServerMessage
{
    public const string TypeDiscriminator = "service-logs-stream-chunk";
}
