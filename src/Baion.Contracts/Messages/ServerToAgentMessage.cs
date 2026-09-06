using System;
using System.Text.Json.Serialization;

namespace Baion.Contracts.Messages;

/// <summary>Raíz de los mensajes que el orquestador envía al agente por el socket.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(WelcomeMessage), WelcomeMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(ConnectionRejectedMessage), ConnectionRejectedMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(ExecuteScriptMessage), ExecuteScriptMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(ForceUpdateMessage), ForceUpdateMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(ListServicesRequestMessage), ListServicesRequestMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(DescribeServiceRequestMessage), DescribeServiceRequestMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(ServiceLogsRequestMessage), ServiceLogsRequestMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(ControlServiceRequestMessage), ControlServiceRequestMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(ServiceLogStreamStartMessage), ServiceLogStreamStartMessage.TypeDiscriminator)]
[JsonDerivedType(typeof(ServiceLogStreamStopMessage), ServiceLogStreamStopMessage.TypeDiscriminator)]
public abstract record ServerToAgentMessage
{
    public Guid MessageId { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset SentAt { get; init; } = DateTimeOffset.UtcNow;
}
