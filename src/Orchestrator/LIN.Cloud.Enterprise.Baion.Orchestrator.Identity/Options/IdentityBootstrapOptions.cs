namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Identity;

// Tenant y usuario administrador que se crean al arrancar si no existen. Resuelve el arranque en frío: sin esto no hay forma de obtener las primeras credenciales. Es idempotente y se desactiva por defecto.
public class IdentityBootstrapOptions
{
    public bool Enabled { get; set; }

    public string TenantName { get; set; } = string.Empty;

    public string TenantSlug { get; set; } = string.Empty;

    public string AdminEmail { get; set; } = string.Empty;

    public string AdminDisplayName { get; set; } = "Administrador";

    // Contraseña inicial. Se usa solo en el primer arranque y debe rotarse después.
    public string AdminPassword { get; set; } = string.Empty;

    public string AdminRole { get; set; } = "Admin";

    public const string SectionName = "Identity:Bootstrap";
}
