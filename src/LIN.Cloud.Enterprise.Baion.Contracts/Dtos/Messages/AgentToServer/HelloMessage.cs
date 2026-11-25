using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record HelloMessage(string ProtocolVersion, ServerPlatform Platform, string RuntimeIdentifier, string AgentVersion, string Hostname, string MachineId, int CoreCount, long TotalMemoryBytes) : AgentToServerMessage
{
    public const string TypeDiscriminator = "hello";
}
