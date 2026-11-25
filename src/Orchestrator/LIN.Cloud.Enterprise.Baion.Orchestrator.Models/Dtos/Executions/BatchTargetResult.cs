using System;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Qué pasó con un miembro del grupo al lanzar el lote.
public record BatchTargetResult(Guid ServerId, string ServerName, BatchTargetOutcome Outcome, Guid? ExecutionId, string? ErrorCode, string? Message);
