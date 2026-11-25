using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

/// <summary>
/// Identidad devuelta por un proveedor tras verificar credenciales, antes de emitir el token.
/// </summary>
public record AuthenticatedUser(Guid UserId, string Email, string DisplayName, Guid SecurityStamp, IReadOnlyList<string> Roles);
