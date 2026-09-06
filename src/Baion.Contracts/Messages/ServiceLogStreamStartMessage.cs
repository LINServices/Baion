using System;

namespace Baion.Contracts.Messages;

/// <summary>
/// Abre un seguimiento en vivo de los logs de un servicio. El agente empuja
/// <see cref="ServiceLogStreamChunkMessage"/> mientras la suscripción siga abierta.
/// </summary>
/// <remarks>Contrato reservado para una fase posterior; el orquestador todavía no lo emite.</remarks>
public record ServiceLogStreamStartMessage(Guid SubscriptionId, string ServiceId) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "service-logs-stream-start";
}
