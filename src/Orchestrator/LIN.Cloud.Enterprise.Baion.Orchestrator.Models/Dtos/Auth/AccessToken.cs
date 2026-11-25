using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record AccessToken(string Value, DateTimeOffset ExpiresAt);
