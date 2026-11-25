using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ScriptSummary(Guid Id, string Name, ScriptRuntime Runtime, int Version, string Checksum, int DefaultTimeoutSeconds);
