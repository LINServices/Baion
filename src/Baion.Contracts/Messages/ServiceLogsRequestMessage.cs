using System;

namespace Baion.Contracts.Messages;

/// <summary>Pide al agente una instantánea de los logs de un servicio.</summary>
/// <param name="MaxLines">Tope de líneas a devolver; el agente además recorta por tamaño de trama.</param>
/// <param name="Since">Si se indica, solo líneas posteriores a ese instante.</param>
public record ServiceLogsRequestMessage(Guid RequestId, string ServiceId, int MaxLines, DateTimeOffset? Since) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "service-logs-request";
}
