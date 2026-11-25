using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

/// <summary>
/// Confirmación de que la orden salió hacia el agente.
/// </summary>
public record ScriptExecutionDispatched(Guid ExecutionId, ExecutionStatus Status);
