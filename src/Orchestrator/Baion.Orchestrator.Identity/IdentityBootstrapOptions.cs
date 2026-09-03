namespace Baion.Orchestrator.Identity;

/// <summary>
/// Tenant y usuario administrador que se crean al arrancar si no existen. Resuelve el arranque en frío:
/// sin esto no hay forma de obtener las primeras credenciales. Es idempotente y se desactiva por defecto.
/// </summary>
public class IdentityBootstrapOptions
{
    public bool Enabled { get; set; }

    public string TenantName { get; set; } = string.Empty;

    public string TenantSlug { get; set; } = string.Empty;

    public string AdminEmail { get; set; } = string.Empty;

    public string AdminDisplayName { get; set; } = "Administrador";

    /// <summary>Contraseña inicial. Se usa solo en el primer arranque y debe rotarse después.</summary>
    public string AdminPassword { get; set; } = string.Empty;

    public string AdminRole { get; set; } = "Admin";

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "Identity:Bootstrap";
}
