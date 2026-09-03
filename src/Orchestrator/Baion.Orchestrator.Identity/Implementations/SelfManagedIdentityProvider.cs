using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Orchestrator.Identity.Implementations;

internal class SelfManagedIdentityProvider(IUserRepository users, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork, IOptions<BaionIdentityOptions> options, TimeProvider timeProvider, ILogger<SelfManagedIdentityProvider> logger) : IIdentityProvider
{
    public IdentityMode Mode => IdentityMode.SelfManaged;

    public async Task<Result<AuthenticatedUser>> VerifyCredentialsAsync(Tenant tenant, LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.GetByNormalizedEmailAsync(NormalizeEmail(request.Email), cancellationToken);

        if (user is null)
        {
            logger.LogWarning("Login rechazado en tenant {TenantId}: el usuario no existe", tenant.Id);
            return InvalidCredentials;
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Login rechazado para el usuario {UserId}: cuenta desactivada", user.Id);
            return Result<AuthenticatedUser>.Failure(Error.Forbidden("auth.user_disabled", "La cuenta está desactivada."));
        }

        var now = timeProvider.GetUtcNow();

        if (user.LockoutEndsAt is DateTimeOffset lockoutEnd && lockoutEnd > now)
        {
            logger.LogWarning("Login rechazado para el usuario {UserId}: bloqueado hasta {LockoutEnd}", user.Id, lockoutEnd);
            return Result<AuthenticatedUser>.Failure(Error.Forbidden("auth.locked_out", "La cuenta está bloqueada temporalmente por intentos fallidos."));
        }

        var verification = passwordHasher.Verify(user.PasswordHash, request.Password);

        if (verification is PasswordVerification.Failed)
        {
            await RegisterFailedAttemptAsync(user, now, cancellationToken);
            logger.LogWarning("Login rechazado para el usuario {UserId}: contraseña incorrecta ({Intentos} intentos)", user.Id, user.AccessFailedCount);
            return InvalidCredentials;
        }

        if (verification is PasswordVerification.SucceededNeedsRehash)
        {
            user.PasswordHash = passwordHasher.Hash(request.Password);
        }

        user.AccessFailedCount = 0;
        user.LockoutEndsAt = null;
        user.LastLoginAt = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var roles = user.UserRoles.Select(userRole => userRole.Role.Name).ToList();

        return Result<AuthenticatedUser>.Success(new AuthenticatedUser(user.Id, user.Email, user.DisplayName, user.SecurityStamp, roles));
    }

    private async Task RegisterFailedAttemptAsync(User user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        user.AccessFailedCount++;

        if (user.AccessFailedCount >= settings.MaxFailedAccessAttempts)
        {
            user.LockoutEndsAt = now.AddMinutes(settings.LockoutMinutes);
            user.AccessFailedCount = 0;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Normaliza el email igual que al darlo de alta, para que la búsqueda por índice único acierte.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    // Mismo error para usuario inexistente y contraseña incorrecta: no se revela qué emails existen.
    private static readonly Result<AuthenticatedUser> InvalidCredentials = Result<AuthenticatedUser>.Failure(Error.Unauthorized("auth.invalid_credentials", "Las credenciales no son válidas."));
}
