using System;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Ficha de un token de instalación. Nunca incluye su valor: de él solo se guarda el hash, así que
// tras emitirlo no hay forma de volver a mostrarlo.
public record EnrollmentTokenSummary(Guid Id, string Name, ServerKind DefaultServerKind, DateTimeOffset? ExpiresAt, int? MaxUses, int UseCount, DateTimeOffset? RevokedAt, DateTimeOffset CreatedAt, bool IsUsable);
