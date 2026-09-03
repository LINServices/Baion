using System;
using Baion.Orchestrator.Models.Enums;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>
/// Token de instalación con el que un agente nuevo se enrola. Se guarda solo su hash: el valor en claro
/// existe una única vez, cuando se emite.
/// </summary>
public class EnrollmentToken : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>SHA-256 del token en hexadecimal. Es la columna por la que se busca al conectarse un agente.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Tipo de servidor que se asigna a los que se enrolen con este token.</summary>
    public ServerKind DefaultServerKind { get; set; } = ServerKind.Vps;

    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Número máximo de enrolamientos permitidos; null significa sin límite.</summary>
    public int? MaxUses { get; set; }

    public int UseCount { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsUsable(DateTimeOffset now) => RevokedAt is null && (ExpiresAt is null || ExpiresAt > now) && (MaxUses is null || UseCount < MaxUses);
}
