using System;

namespace Baion.Contracts.Messages;

/// <summary>Cierra un seguimiento en vivo de logs abierto con <see cref="ServiceLogStreamStartMessage"/>.</summary>
/// <remarks>Contrato reservado para una fase posterior; el orquestador todavía no lo emite.</remarks>
public record ServiceLogStreamStopMessage(Guid SubscriptionId) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "service-logs-stream-stop";
}
