using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

/// <summary>Primer mensaje del agente tras abrir el socket: describe la máquina donde corre.</summary>
public record HelloMessage(string ProtocolVersion, ServerPlatform Platform, string RuntimeIdentifier, string AgentVersion, string Hostname, string MachineId, int CoreCount, long TotalMemoryBytes) : AgentToServerMessage
{
    public const string TypeDiscriminator = "hello";
}
