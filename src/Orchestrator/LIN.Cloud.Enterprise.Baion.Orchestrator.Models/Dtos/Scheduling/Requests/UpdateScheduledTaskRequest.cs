using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Reemplaza la definición de una tarea; misma forma y mismas reglas que al crearla.
public record UpdateScheduledTaskRequest(string Name, string CronExpression, string TimeZoneId, Guid? ScriptId, Guid? ScriptChainId, Guid? ServerId, Guid? ServerGroupId, ExecutionMode Mode, int OfflineGraceSeconds);
