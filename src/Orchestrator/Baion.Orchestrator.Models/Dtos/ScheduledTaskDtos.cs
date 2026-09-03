using System;
using Baion.Contracts.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>
/// Datos para programar una tarea. El destino y la carga son excluyentes: exactamente uno de cada par.
/// </summary>
/// <param name="OfflineGraceSeconds">Margen para que un agente desconectado vuelva; cero falla en el acto.</param>
public record CreateScheduledTaskRequest(string Name, string CronExpression, string TimeZoneId, Guid? ScriptId, Guid? ScriptChainId, Guid? ServerId, Guid? ServerGroupId, ExecutionMode Mode, int OfflineGraceSeconds);

/// <summary>Ficha de una tarea programada.</summary>
public record ScheduledTaskSummary(Guid Id, string Name, string CronExpression, string TimeZoneId, Guid? ScriptId, Guid? ScriptChainId, Guid? ServerId, Guid? ServerGroupId, ExecutionMode Mode, bool IsEnabled, DateTimeOffset? LastRunAt, DateTimeOffset? NextRunAt);

/// <summary>Resultado de un disparo: qué se lanzó sobre cuántos servidores.</summary>
public record ScheduledTaskTriggered(Guid ScheduledTaskId, int TargetCount, int DispatchedCount, int FailedCount);
