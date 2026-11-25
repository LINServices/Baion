using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record CreateUserRequest(string Email, string DisplayName, string Password, IReadOnlyList<string> Roles);
