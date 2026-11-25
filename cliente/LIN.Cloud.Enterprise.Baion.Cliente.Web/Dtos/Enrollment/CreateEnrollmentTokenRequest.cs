using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Enrollment;

public record CreateEnrollmentTokenRequest(string Name, string DefaultServerKind, DateTimeOffset? ExpiresAt, int? MaxUses);
