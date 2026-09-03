using System;
using System.Collections.Generic;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Respuesta de un login correcto.</summary>
public record AuthenticationResult(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, Guid TenantId, Guid UserId, string Email, IReadOnlyList<string> Roles);
