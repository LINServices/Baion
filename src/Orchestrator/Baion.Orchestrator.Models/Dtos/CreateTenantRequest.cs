using Baion.Orchestrator.Models.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Datos para dar de alta un tenant.</summary>
public record CreateTenantRequest(string Name, string Slug, IdentityMode IdentityMode, string? ExternalTenantId);
