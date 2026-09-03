using System;
using Baion.Contracts.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>
/// Novedad de una ejecución llegada por el socket. Salida y desenlace comparten un único buzón para que
/// se escriban en el mismo orden en que ocurrieron: nadie puede ver una ejecución terminada con la salida
/// todavía a medias.
/// </summary>
public abstract record ScriptExecutionEvent(Guid TenantId, Guid ExecutionId);

/// <summary>El proceso arrancó en la máquina.</summary>
public record ScriptStartEvent(Guid TenantId, Guid ExecutionId, DateTimeOffset StartedAt) : ScriptExecutionEvent(TenantId, ExecutionId);

/// <summary>Fragmento de salida.</summary>
public record ScriptOutputEvent(Guid TenantId, Guid ExecutionId, OutputStream Stream, string Content) : ScriptExecutionEvent(TenantId, ExecutionId);

/// <summary>Desenlace de la ejecución.</summary>
public record ScriptCompletionEvent(Guid TenantId, Guid ExecutionId, ExecutionStatus Status, int? ExitCode, DateTimeOffset CompletedAt, string? ErrorMessage) : ScriptExecutionEvent(TenantId, ExecutionId);
