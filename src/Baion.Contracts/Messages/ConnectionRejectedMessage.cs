namespace Baion.Contracts.Messages;

/// <summary>Rechazo del handshake una vez abierto el socket, antes de cerrarlo.</summary>
public record ConnectionRejectedMessage(string Code, string Reason) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "connection-rejected";
}
