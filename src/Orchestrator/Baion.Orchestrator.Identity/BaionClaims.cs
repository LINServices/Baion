namespace Baion.Orchestrator.Identity;

/// <summary>Nombres de claim del token de Baion, compartidos entre quien lo emite y quien lo valida.</summary>
public static class BaionClaims
{
    public const string Subject = "sub";

    public const string Email = "email";

    public const string TenantId = "tid";

    /// <summary>Copia del <c>SecurityStamp</c> del usuario; permite invalidar tokens ya emitidos.</summary>
    public const string SecurityStamp = "stamp";

    public const string Roles = "roles";

    public const string TokenId = "jti";
}
