using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Novedad de una ejecución llegada por el socket. Salida y desenlace comparten un único buzón para que
// se escriban en el mismo orden en que ocurrieron: nadie puede ver una ejecución terminada con la salida
// todavía a medias.
public abstract record ScriptExecutionEvent(Guid TenantId, Guid ExecutionId);
