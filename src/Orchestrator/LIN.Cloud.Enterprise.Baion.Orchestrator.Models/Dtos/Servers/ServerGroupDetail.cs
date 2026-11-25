using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ServerGroupDetail(Guid Id, string Name, string? Description, IReadOnlyList<ServerGroupMemberSummary> Members, DateTimeOffset CreatedAt);
