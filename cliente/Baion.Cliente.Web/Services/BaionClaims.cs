namespace Baion.Cliente.Web.Services;

/// <summary>Claims que el panel guarda en su cookie de sesión.</summary>
public static class BaionClaims
{
    /// <summary>
    /// Token de acceso de la API. Viaja dentro de la cookie de autenticación, que va cifrada y marcada
    /// como HttpOnly: nunca llega al JavaScript del navegador.
    /// </summary>
    public const string AccessToken = "baion:access_token";

    public const string TenantId = "baion:tenant_id";

    public const string ExpiresAt = "baion:expires_at";
}
