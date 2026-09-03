namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Credenciales enviadas al endpoint de login. El slug identifica al tenant contra el que se autentica.</summary>
public record LoginRequest(string TenantSlug, string Email, string Password);
