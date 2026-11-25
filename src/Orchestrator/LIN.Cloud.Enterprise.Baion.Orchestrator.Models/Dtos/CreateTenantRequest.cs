using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

/// <summary>
/// Datos para dar de alta un tenant.
/// </summary>
public record CreateTenantRequest(string Name, string Slug, IdentityMode IdentityMode, string? ExternalTenantId);
