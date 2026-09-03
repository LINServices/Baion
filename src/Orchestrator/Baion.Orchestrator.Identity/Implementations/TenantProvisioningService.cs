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

namespace Baion.Orchestrator.Identity.Implementations;

internal class TenantProvisioningService(ITenantRepository tenants, IUnitOfWork unitOfWork, ILogger<TenantProvisioningService> logger) : ITenantProvisioningService
{
    public async Task<Result<Guid>> EnsureTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<Guid>.Failure(Error.Validation("tenant.name_required", "El nombre del tenant es obligatorio."));
        }

        var slug = AuthenticationService.NormalizeSlug(request.Slug ?? string.Empty);

        if (!IsValidSlug(slug))
        {
            return Result<Guid>.Failure(Error.Validation("tenant.slug_invalid", "El slug solo admite minúsculas, dígitos y guiones, entre 3 y 100 caracteres."));
        }

        if (request.IdentityMode is IdentityMode.Lin && string.IsNullOrWhiteSpace(request.ExternalTenantId))
        {
            return Result<Guid>.Failure(Error.Validation("tenant.external_id_required", "Un tenant en modo LIN necesita su identificador externo."));
        }

        var existing = await tenants.GetBySlugAsync(slug);

        if (existing is not null)
        {
            return Result<Guid>.Success(existing.Id);
        }

        var tenant = new Tenant
        {
            Name = request.Name.Trim(),
            Slug = slug,
            IdentityMode = request.IdentityMode,
            ExternalTenantId = request.ExternalTenantId?.Trim()
        };

        await tenants.AddAsync(tenant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Tenant {TenantId} creado con slug {TenantSlug}", tenant.Id, tenant.Slug);

        return Result<Guid>.Success(tenant.Id);
    }

    private static bool IsValidSlug(string slug) => slug.Length is >= 3 and <= 100 && slug.All(character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-');
}
