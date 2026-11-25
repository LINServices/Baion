using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.ServerGroups;

public record ServerGroupMemberSummary(Guid ServerId, string Name, string Hostname, string Platform, string Status);
