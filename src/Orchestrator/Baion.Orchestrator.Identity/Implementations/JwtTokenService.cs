using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Results;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Baion.Orchestrator.Identity.Implementations;

internal class JwtTokenService(IOptions<BaionIdentityOptions> options, TimeProvider timeProvider) : ITokenService
{
    public AccessToken Issue(Guid tenantId, AuthenticatedUser user)
    {
        var settings = options.Value;
        var issuedAt = timeProvider.GetUtcNow();
        var expiresAt = issuedAt.AddMinutes(settings.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(CreateSigningKey(settings.SigningKey), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [BaionClaims.Subject] = user.UserId.ToString(),
                [BaionClaims.Email] = user.Email,
                [BaionClaims.TenantId] = tenantId.ToString(),
                [BaionClaims.SecurityStamp] = user.SecurityStamp.ToString(),
                [BaionClaims.Roles] = user.Roles,
                [BaionClaims.TokenId] = Guid.NewGuid().ToString()
            }
        };

        return new AccessToken(Handler.CreateToken(descriptor), expiresAt);
    }

    public async Task<Result<BaionPrincipal>> ValidateAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return InvalidToken;
        }

        var settings = options.Value;

        var parameters = new TokenValidationParameters
        {
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = CreateSigningKey(settings.SigningKey),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = ClockSkew
        };

        var validation = await Handler.ValidateTokenAsync(token, parameters);

        if (!validation.IsValid || validation.SecurityToken is not JsonWebToken jwt)
        {
            return InvalidToken;
        }

        if (!jwt.TryGetPayloadValue<string>(BaionClaims.Subject, out var subject) || !Guid.TryParse(subject, out var userId))
        {
            return InvalidToken;
        }

        if (!jwt.TryGetPayloadValue<string>(BaionClaims.TenantId, out var tenant) || !Guid.TryParse(tenant, out var tenantId))
        {
            return InvalidToken;
        }

        if (!jwt.TryGetPayloadValue<string>(BaionClaims.SecurityStamp, out var stamp) || !Guid.TryParse(stamp, out var securityStamp))
        {
            return InvalidToken;
        }

        jwt.TryGetPayloadValue<string>(BaionClaims.Email, out var email);

        var roles = jwt.TryGetPayloadValue<string[]>(BaionClaims.Roles, out var claimed) ? claimed : [];

        return Result<BaionPrincipal>.Success(new BaionPrincipal(tenantId, userId, email ?? string.Empty, securityStamp, roles));
    }

    private static SymmetricSecurityKey CreateSigningKey(string signingKey) => new(Encoding.UTF8.GetBytes(signingKey));

    private static readonly JsonWebTokenHandler Handler = new();

    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    // Un único error para cualquier motivo de rechazo: no se revela por qué falló la validación.
    private static readonly Result<BaionPrincipal> InvalidToken = Result<BaionPrincipal>.Failure(Error.Unauthorized("token.invalid", "El token no es válido o expiró."));
}
