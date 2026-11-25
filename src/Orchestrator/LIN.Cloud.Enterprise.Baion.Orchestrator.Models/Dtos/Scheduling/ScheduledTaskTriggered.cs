using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Resultado de un disparo: qué se lanzó sobre cuántos servidores.
// SkippedCount: omitidos por incompatibilidad o servidor deshabilitado; DispatchedCount + FailedCount + SkippedCount == TargetCount.
public record ScheduledTaskTriggered(Guid ScheduledTaskId, int TargetCount, int DispatchedCount, int FailedCount, int SkippedCount);
