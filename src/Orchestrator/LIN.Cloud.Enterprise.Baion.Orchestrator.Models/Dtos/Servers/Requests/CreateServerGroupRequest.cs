using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// ServerIds: null equivale a un grupo sin miembros; los repetidos se deduplican.
public record CreateServerGroupRequest(string Name, string? Description, IReadOnlyList<Guid>? ServerIds);
