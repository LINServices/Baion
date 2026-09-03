using Baion.Orchestrator.Models.Enums;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Organización dueña de servidores, scripts y tareas dentro de Baion.</summary>
public class Tenant : Entity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Identificador legible y único del tenant, usado en URLs y en el enrolamiento.</summary>
    public string Slug { get; set; } = string.Empty;

    public IdentityMode IdentityMode { get; set; } = IdentityMode.SelfManaged;

    /// <summary>Identificador del tenant en LIN Cloud Identity Platform, cuando <see cref="IdentityMode"/> es Lin.</summary>
    public string? ExternalTenantId { get; set; }

    public bool IsActive { get; set; } = true;
}
