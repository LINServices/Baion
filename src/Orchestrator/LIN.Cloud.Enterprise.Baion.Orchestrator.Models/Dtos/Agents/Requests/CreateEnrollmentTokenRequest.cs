using System;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record CreateEnrollmentTokenRequest(string Name, ServerKind DefaultServerKind, DateTimeOffset? ExpiresAt, int? MaxUses);
