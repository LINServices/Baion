using System;
using System.Text.Json.Serialization;

namespace Baion.Contracts.Messages;

/// <summary>Raíz de los mensajes que el orquestador envía al agente por el socket.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(WelcomeMessage), WelcomeMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(ConnectionRejectedMessage), ConnectionRejectedMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(ExecuteScriptMessage), ExecuteScriptMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(ForceUpdateMessage), ForceUpdateMessage.TypeDiscriminator)]
public abstract record ServerToAgentMessage
{
    public Guid MessageId { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset SentAt { get; init; } = DateTimeOffset.UtcNow;
}
