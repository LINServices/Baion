using System;
using Baion.Contracts.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Confirmación de que la orden salió hacia el agente.</summary>
public record ScriptExecutionDispatched(Guid ExecutionId, ExecutionStatus Status);
