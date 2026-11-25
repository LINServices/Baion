using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Resultado de un disparo: qué se lanzó sobre cuántos servidores.
public record ScheduledTaskTriggered(Guid ScheduledTaskId, int TargetCount, int DispatchedCount, int FailedCount);
