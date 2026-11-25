using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Enrollment;

// El valor en claro solo llega aquí y no se puede volver a consultar.
public record EnrollmentTokenIssued(Guid Id, string Token, DateTimeOffset? ExpiresAt);
