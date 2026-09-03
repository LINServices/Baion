namespace Baion.Contracts.Messages;

/// <summary>Ordena al agente descargar la versión indicada para su RID, reemplazarse y reconectar.</summary>
public record ForceUpdateMessage(string TargetVersion, string DownloadUrlTemplate, string? ExpectedChecksum) : ServerToAgentMessage
{
    public const string TypeDiscriminator = "force-update";

    /// <summary>Marcador que el agente reemplaza por su propio RID dentro de <see cref="DownloadUrlTemplate"/>.</summary>
    public const string RidPlaceholder = "{rid}";
}
