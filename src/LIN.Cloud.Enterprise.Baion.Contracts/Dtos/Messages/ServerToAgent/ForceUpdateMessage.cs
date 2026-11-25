namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ForceUpdateMessage(string TargetVersion, string DownloadUrlTemplate, string? ExpectedChecksum) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "force-update";

    public const string RidPlaceholder = "{rid}";
}
