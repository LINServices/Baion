namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Auth;

public record LoginRequest(string TenantSlug, string Email, string Password);
