using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ServerGroupMemberSummary(Guid ServerId, string Name, string Hostname, ServerPlatform Platform, ServerStatus Status);
