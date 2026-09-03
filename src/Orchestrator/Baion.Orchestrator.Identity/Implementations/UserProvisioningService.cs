using System;
using System.Collections.Generic;
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

internal class UserProvisioningService(ITenantRepository tenants, ITenantContext tenantContext, IUserRepository users, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork, IOptions<BaionIdentityOptions> options, ILogger<UserProvisioningService> logger) : IUserProvisioningService
{
    public async Task<Result<Guid>> CreateUserAsync(Guid tenantId, CreateUserRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return Result<Guid>.Failure(Error.Validation("user.email_invalid", "El email no es válido."));
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Result<Guid>.Failure(Error.Validation("user.display_name_required", "El nombre para mostrar es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < settings.MinimumPasswordLength)
        {
            return Result<Guid>.Failure(Error.Validation("user.password_too_short", $"La contraseña debe tener al menos {settings.MinimumPasswordLength} caracteres."));
        }

        var tenant = await tenants.GetByIdAsync(tenantId);

        if (tenant is null)
        {
            return Result<Guid>.Failure(Error.NotFound("tenant.not_found", "El tenant no existe."));
        }

        if (tenant.IdentityMode is not IdentityMode.SelfManaged)
        {
            return Result<Guid>.Failure(Error.Conflict("tenant.not_self_managed", "Los usuarios de un tenant en modo LIN se administran en LIN Cloud Identity Platform."));
        }

        tenantContext.SetTenant(tenant.Id);

        var normalizedEmail = SelfManagedIdentityProvider.NormalizeEmail(request.Email);

        if (await users.ExistsByNormalizedEmailAsync(normalizedEmail))
        {
            return Result<Guid>.Failure(Error.Conflict("user.email_taken", "Ya existe un usuario con ese email en el tenant."));
        }

        var user = new User
        {
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password)
        };

        foreach (var role in await ResolveRolesAsync(request.Roles, cancellationToken))
        {
            user.UserRoles.Add(new UserRole { User = user, Role = role });
        }

        await users.AddAsync(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Usuario {UserId} creado en el tenant {TenantId}", user.Id, tenant.Id);

        return Result<Guid>.Success(user.Id);
    }

    /// <summary>Devuelve los roles pedidos, creando los que el tenant todavía no tenga.</summary>
    private async Task<IReadOnlyList<Role>> ResolveRolesAsync(IReadOnlyList<string> requested, CancellationToken cancellationToken)
    {
        var normalized = requested
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .DistinctBy(role => role.ToUpperInvariant())
            .ToList();

        if (normalized.Count == 0)
        {
            return [];
        }

        var existing = await users.GetRolesByNormalizedNamesAsync(normalized.Select(role => role.ToUpperInvariant()).ToList(), cancellationToken);

        var faltantes = normalized
            .Where(role => !existing.Any(candidate => candidate.NormalizedName == role.ToUpperInvariant()))
            .Select(role => new Role { Name = role, NormalizedName = role.ToUpperInvariant() })
            .ToList();

        return [.. existing, .. faltantes];
    }
}
