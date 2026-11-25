namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Identity;

public class BaionIdentityOptions
{
    public string Issuer { get; set; } = "baion";

    public string Audience { get; set; } = "baion";

    // Clave simétrica de firma. Nunca debe versionarse: va por secreto o variable de entorno.
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;

    public int MinimumPasswordLength { get; set; } = 12;

    public int MaxFailedAccessAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    public const string SectionName = "Identity";
}
