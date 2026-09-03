using System;
using System.Collections.Generic;
using System.Linq;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Identidad extraída de un token de acceso ya validado.</summary>
public record BaionPrincipal(Guid TenantId, Guid UserId, string Email, Guid SecurityStamp, IReadOnlyList<string> Roles)
{
    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
