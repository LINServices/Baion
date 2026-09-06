namespace Baion.Cliente.Web.Services;

/// <summary>Claims con los que el panel arma la identidad del usuario a partir de la sesión guardada.</summary>
public static class BaionClaims
{
    /// <summary>
    /// Token de acceso de la API. En WebAssembly la sesión vive en el navegador, así que este valor es
    /// accesible desde el propio panel; nunca se envía a un tercero que no sea el orquestador.
    /// </summary>
    public const string AccessToken = "baion:access_token";

    public const string TenantId = "baion:tenant_id";

    public const string ExpiresAt = "baion:expires_at";
}
