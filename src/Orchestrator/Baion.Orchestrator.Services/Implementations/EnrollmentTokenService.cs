using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Persistence;
using Microsoft.Extensions.Logging;

namespace Baion.Orchestrator.Services.Implementations;

internal class EnrollmentTokenService(IRepository<EnrollmentToken> tokens, IUnitOfWork unitOfWork, TimeProvider timeProvider, ILogger<EnrollmentTokenService> logger) : IEnrollmentTokenService
{
    public async Task<Result<EnrollmentTokenIssued>> CreateAsync(CreateEnrollmentTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<EnrollmentTokenIssued>.Failure(Error.Validation("enrollment_token.name_required", "El nombre del token es obligatorio."));
        }

        if (request.MaxUses is <= 0)
        {
            return Result<EnrollmentTokenIssued>.Failure(Error.Validation("enrollment_token.max_uses_invalid", "El número máximo de usos debe ser mayor que cero."));
        }

        if (request.ExpiresAt is DateTimeOffset expiresAt && expiresAt <= timeProvider.GetUtcNow())
        {
            return Result<EnrollmentTokenIssued>.Failure(Error.Validation("enrollment_token.already_expired", "La fecha de expiración ya pasó."));
        }

        var value = AgentTokens.Generate();

        var token = new EnrollmentToken
        {
            Name = request.Name.Trim(),
            TokenHash = AgentTokens.Hash(value),
            DefaultServerKind = request.DefaultServerKind,
            ExpiresAt = request.ExpiresAt,
            MaxUses = request.MaxUses
        };

        await tokens.AddAsync(token);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Token de instalación {TokenId} emitido con nombre {TokenName}", token.Id, token.Name);

        return Result<EnrollmentTokenIssued>.Success(new EnrollmentTokenIssued(token.Id, value, token.ExpiresAt));
    }

    // Son un puñado de filas por tenant, así que se proyecta en memoria en lugar de montar una
    // interfaz de consultas aparte solo para esto.
    public async Task<IReadOnlyList<EnrollmentTokenSummary>> ListAsync(CancellationToken cancellationToken)
    {
        var ahora = timeProvider.GetUtcNow();

        return [.. (await tokens.GetAllAsync(cancellationToken))
            .OrderByDescending(token => token.CreatedAt)
            .Select(token => new EnrollmentTokenSummary(token.Id, token.Name, token.DefaultServerKind, token.ExpiresAt, token.MaxUses, token.UseCount, token.RevokedAt, token.CreatedAt, token.IsUsable(ahora)))];
    }

    public async Task<Result> RevokeAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        var token = await tokens.GetByIdAsync(tokenId);

        if (token is null)
        {
            return Result.Failure(Error.NotFound("enrollment_token.not_found", "El token de instalación no existe."));
        }

        if (token.RevokedAt is not null)
        {
            return Result.Success();
        }

        token.RevokedAt = timeProvider.GetUtcNow();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
