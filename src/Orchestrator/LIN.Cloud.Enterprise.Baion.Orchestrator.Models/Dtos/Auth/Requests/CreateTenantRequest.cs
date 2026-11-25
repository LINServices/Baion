using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record CreateTenantRequest(string Name, string Slug, IdentityMode IdentityMode, string? ExternalTenantId);
