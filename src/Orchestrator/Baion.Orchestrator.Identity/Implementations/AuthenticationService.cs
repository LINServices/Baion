using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Persistence;
using Microsoft.Extensions.Logging;

namespace Baion.Orchestrator.Identity.Implementations;

internal class AuthenticationService(ITenantRepository tenants, ITenantContext tenantContext, ITokenService tokenService, IEnumerable<IIdentityProvider> providers, ILogger<AuthenticationService> logger) : IAuthenticationService
{
    public async Task<Result<AuthenticationResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TenantSlug))
        {
            return Result<AuthenticationResult>.Failure(Error.Validation("auth.tenant_required", "El slug del tenant es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<AuthenticationResult>.Failure(Error.Validation("auth.email_required", "El email es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthenticationResult>.Failure(Error.Validation("auth.password_required", "La contraseña es obligatoria."));
        }

        var tenant = await tenants.GetBySlugAsync(NormalizeSlug(request.TenantSlug));

        // Mismo error que unas credenciales incorrectas: el login no revela qué tenants existen.
        if (tenant is null || !tenant.IsActive)
        {
            logger.LogWarning("Login rechazado: tenant '{TenantSlug}' inexistente o inactivo", request.TenantSlug);
            return InvalidCredentials;
        }

        tenantContext.SetTenant(tenant.Id);

        var provider = providers.FirstOrDefault(candidate => candidate.Mode == tenant.IdentityMode);

        if (provider is null)
        {
            logger.LogError("El tenant {TenantId} está en modo {IdentityMode} y no hay proveedor registrado", tenant.Id, tenant.IdentityMode);
            return Result<AuthenticationResult>.Failure(Error.Unexpected("identity.provider_missing", "No hay un proveedor de identidad para el modo del tenant."));
        }

        var verification = await provider.VerifyCredentialsAsync(tenant, request, cancellationToken);

        if (verification is not { IsSuccess: true, Value: AuthenticatedUser user })
        {
            return Result<AuthenticationResult>.Failure(verification.Error!);
        }

        var token = tokenService.Issue(tenant.Id, user);
        logger.LogInformation("Login correcto del usuario {UserId} en el tenant {TenantId}", user.UserId, tenant.Id);

        return Result<AuthenticationResult>.Success(new AuthenticationResult(token.Value, TokenType, token.ExpiresAt, tenant.Id, user.UserId, user.Email, user.Roles));
    }

    /// <summary>Normaliza el slug igual que al crear el tenant, para que el índice único acierte.</summary>
    public static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant();

    private const string TokenType = "Bearer";

    private static readonly Result<AuthenticationResult> InvalidCredentials = Result<AuthenticationResult>.Failure(Error.Unauthorized("auth.invalid_credentials", "Las credenciales no son válidas."));
}
