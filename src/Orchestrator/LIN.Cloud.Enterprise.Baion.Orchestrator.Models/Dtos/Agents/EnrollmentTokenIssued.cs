using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Token de instalación recién emitido. El valor en claro solo se devuelve aquí y no se guarda.
public record EnrollmentTokenIssued(Guid Id, string Token, DateTimeOffset? ExpiresAt);
