using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Auth;

public record AuthenticationResult(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, Guid TenantId, Guid UserId, string Email, IReadOnlyList<string> Roles);
