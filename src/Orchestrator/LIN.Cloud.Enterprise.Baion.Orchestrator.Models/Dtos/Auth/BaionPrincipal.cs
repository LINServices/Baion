using System;
using System.Collections.Generic;
using System.Linq;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record BaionPrincipal(Guid TenantId, Guid UserId, string Email, Guid SecurityStamp, IReadOnlyList<string> Roles)
{
    public bool IsInRole(string role)
    {
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
