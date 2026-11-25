using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// ServerIds reemplaza por completo los miembros del grupo.
public record UpdateServerGroupRequest(string Name, string? Description, IReadOnlyList<Guid>? ServerIds);
