namespace Baion.Orchestrator.Identity;

/// <summary>Parámetros de emisión de tokens y de política de contraseñas.</summary>
public class BaionIdentityOptions
{
    public string Issuer { get; set; } = "baion";

    public string Audience { get; set; } = "baion";

    /// <summary>Clave simétrica de firma. Nunca debe versionarse: va por secreto o variable de entorno.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;

    public int MinimumPasswordLength { get; set; } = 12;

    /// <summary>Intentos fallidos consecutivos tras los que la cuenta queda bloqueada.</summary>
    public int MaxFailedAccessAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "Identity";
}
