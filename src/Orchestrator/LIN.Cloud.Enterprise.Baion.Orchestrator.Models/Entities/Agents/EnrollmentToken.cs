using System;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

// Token de instalación con el que un agente nuevo se enrola. Se guarda solo su hash: el valor en claro
// existe una única vez, cuando se emite.
public class EnrollmentToken : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    // SHA-256 del token en hexadecimal. Es la columna por la que se busca al conectarse un agente.
    public string TokenHash { get; set; } = string.Empty;

    // Tipo de servidor que se asigna a los que se enrolen con este token.
    public ServerKind DefaultServerKind { get; set; } = ServerKind.Vps;

    public DateTimeOffset? ExpiresAt { get; set; }

    // Número máximo de enrolamientos permitidos; null significa sin límite.
    public int? MaxUses { get; set; }

    public int UseCount { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsUsable(DateTimeOffset now)
    {
        return RevokedAt is null && (ExpiresAt is null || ExpiresAt > now) && (MaxUses is null || UseCount < MaxUses);
    }
}
