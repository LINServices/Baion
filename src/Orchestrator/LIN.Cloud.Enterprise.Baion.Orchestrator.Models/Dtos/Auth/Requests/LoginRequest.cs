namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Credenciales enviadas al endpoint de login. El slug identifica al tenant contra el que se autentica.
public record LoginRequest(string TenantSlug, string Email, string Password);
