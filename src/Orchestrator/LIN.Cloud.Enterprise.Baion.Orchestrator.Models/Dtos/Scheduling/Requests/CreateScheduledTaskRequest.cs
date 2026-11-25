using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Datos para programar una tarea. El destino y la carga son excluyentes: exactamente uno de cada par.
// OfflineGraceSeconds: Margen para que un agente desconectado vuelva; cero falla en el acto.
public record CreateScheduledTaskRequest(string Name, string CronExpression, string TimeZoneId, Guid? ScriptId, Guid? ScriptChainId, Guid? ServerId, Guid? ServerGroupId, ExecutionMode Mode, int OfflineGraceSeconds);
