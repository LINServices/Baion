namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Identity;

public static class BaionClaims
{
    public const string Subject = "sub";

    public const string Email = "email";

    public const string TenantId = "tid";

    // Copia del SecurityStamp del usuario; permite invalidar tokens ya emitidos.
    public const string SecurityStamp = "stamp";

    public const string Roles = "roles";

    public const string TokenId = "jti";
}
