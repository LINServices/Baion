using System.Collections.Generic;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Datos para dar de alta un usuario en un tenant autogestionado.</summary>
public record CreateUserRequest(string Email, string DisplayName, string Password, IReadOnlyList<string> Roles);
