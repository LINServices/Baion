using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

/// <summary>Ordena al agente aplicar una acción sobre un servicio y devolver su detalle resultante.</summary>
public record ControlServiceRequestMessage(Guid RequestId, string ServiceId, ServiceControlAction Action) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "service-control-request";
}
