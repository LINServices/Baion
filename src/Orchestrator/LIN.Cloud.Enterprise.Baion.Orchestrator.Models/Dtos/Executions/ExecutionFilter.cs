using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Criterios de búsqueda de ejecuciones; cada campo nulo no filtra.
public record ExecutionFilter(Guid? ServerId, Guid? ScriptId, ExecutionStatus? Status, DateTimeOffset? Since);
