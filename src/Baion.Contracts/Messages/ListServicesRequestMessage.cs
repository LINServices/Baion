using System;

namespace Baion.Contracts.Messages;

/// <summary>Pide al agente el listado de servicios del sistema.</summary>
/// <param name="NameFilter">Subcadena para acotar por nombre; null trae todos.</param>
public record ListServicesRequestMessage(Guid RequestId, string? NameFilter) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "services-list-request";
}
