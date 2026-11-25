using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

public class Tenant : Entity
{
    public string Name { get; set; } = string.Empty;

    // Identificador legible y único del tenant, usado en URLs y en el enrolamiento.
    public string Slug { get; set; } = string.Empty;

    public IdentityMode IdentityMode { get; set; } = IdentityMode.SelfManaged;

    // Identificador del tenant en LIN Cloud Identity Platform, cuando IdentityMode es Lin.
    public string? ExternalTenantId { get; set; }

    public bool IsActive { get; set; } = true;
}
