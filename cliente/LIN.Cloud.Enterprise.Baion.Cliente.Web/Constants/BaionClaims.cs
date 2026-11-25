namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Constants;

public static class BaionClaims
{
    // El token es accesible desde el navegador (WebAssembly); nunca se envía a un tercero que no sea el orquestador.
    public const string AccessToken = "baion:access_token";

    public const string TenantId = "baion:tenant_id";

    public const string ExpiresAt = "baion:expires_at";
}
