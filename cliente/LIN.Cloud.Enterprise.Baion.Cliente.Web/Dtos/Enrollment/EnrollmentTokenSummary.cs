using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Enrollment;

public record EnrollmentTokenSummary(Guid Id, string Name, string DefaultServerKind, DateTimeOffset? ExpiresAt, int? MaxUses, int UseCount, DateTimeOffset? RevokedAt, DateTimeOffset CreatedAt, bool IsUsable);
