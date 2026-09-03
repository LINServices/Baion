using System;
using Baion.Orchestrator.Models.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Token de instalación recién emitido. El valor en claro solo se devuelve aquí y no se guarda.</summary>
public record EnrollmentTokenIssued(Guid Id, string Token, DateTimeOffset? ExpiresAt);

/// <summary>
/// Ficha de un token de instalación. Nunca incluye su valor: de él solo se guarda el hash, así que
/// tras emitirlo no hay forma de volver a mostrarlo.
/// </summary>
public record EnrollmentTokenSummary(Guid Id, string Name, ServerKind DefaultServerKind, DateTimeOffset? ExpiresAt, int? MaxUses, int UseCount, DateTimeOffset? RevokedAt, DateTimeOffset CreatedAt, bool IsUsable);
