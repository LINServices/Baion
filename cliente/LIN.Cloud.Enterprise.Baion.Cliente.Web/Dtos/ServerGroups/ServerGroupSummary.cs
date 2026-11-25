using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.ServerGroups;

public record ServerGroupSummary(Guid Id, string Name, string? Description, int MemberCount, int LinuxCount, int WindowsCount, DateTimeOffset CreatedAt);
