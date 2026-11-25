using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Services;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

// Contrato reservado para una fase posterior; el agente todavía no lo emite
public record ServiceLogStreamChunkMessage(Guid SubscriptionId, IReadOnlyList<ServiceLogLine> Lines) : AgentToServerMessage
{
    public const string TypeDiscriminator = "service-logs-stream-chunk";
}
