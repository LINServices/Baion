using System;
using System.Text.Json.Serialization;

namespace Baion.Contracts.Messages;

/// <summary>Raíz de los mensajes que el agente envía al orquestador por el socket.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(HelloMessage), HelloMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(HeartbeatMessage), HeartbeatMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(MetricsReportMessage), MetricsReportMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(ScriptStartedMessage), ScriptStartedMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(ScriptOutputChunkMessage), ScriptOutputChunkMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(ScriptCompletedMessage), ScriptCompletedMessage.TypeDiscriminator)]
public abstract record AgentToServerMessage
{
    public Guid MessageId { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset SentAt { get; init; } = DateTimeOffset.UtcNow;
}
