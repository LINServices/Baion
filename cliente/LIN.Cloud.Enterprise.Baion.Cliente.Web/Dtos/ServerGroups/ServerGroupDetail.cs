using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.ServerGroups;

public record ServerGroupDetail(Guid Id, string Name, string? Description, IReadOnlyList<ServerGroupMemberSummary> Members, DateTimeOffset CreatedAt);
