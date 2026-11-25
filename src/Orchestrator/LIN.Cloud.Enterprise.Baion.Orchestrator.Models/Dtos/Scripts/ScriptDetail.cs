using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ScriptDetail(Guid Id, string Name, string? Description, string Content, ScriptRuntime Runtime, int Version, string Checksum, int DefaultTimeoutSeconds, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
