using System;
using Baion.Orchestrator.Models.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Datos para emitir un token de instalación.</summary>
public record CreateEnrollmentTokenRequest(string Name, ServerKind DefaultServerKind, DateTimeOffset? ExpiresAt, int? MaxUses);
