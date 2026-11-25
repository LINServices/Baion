using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.ServerGroups;

public record CreateServerGroupRequest(string Name, string? Description, IReadOnlyList<Guid>? ServerIds);
