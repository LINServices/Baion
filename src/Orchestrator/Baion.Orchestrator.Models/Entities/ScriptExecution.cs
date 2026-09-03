using System;
using Baion.Contracts.Enums;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Resultado de ejecutar un script sobre un servidor concreto.</summary>
public class ScriptExecution : TenantEntity
{
    public Guid ServerId { get; set; }

    public Server Server { get; set; } = null!;

    public Guid ScriptId { get; set; }

    public Script Script { get; set; } = null!;

    /// <summary>Paso de cadena que originó la ejecución, cuando no es una ejecución suelta.</summary>
    public Guid? ScriptChainStepId { get; set; }

    public ScriptChainStep? ScriptChainStep { get; set; }

    /// <summary>Correlaciona las ejecuciones de un mismo recorrido de cadena, incluso si hay varios en paralelo.</summary>
    public Guid? ChainRunId { get; set; }

    /// <summary>Tarea programada que originó la ejecución, cuando no la pidió una persona.</summary>
    public Guid? ScheduledTaskId { get; set; }

    public ScheduledTask? ScheduledTask { get; set; }

    /// <summary>
    /// Instante hasta el que se sigue intentando entregar la orden a un agente desconectado. Solo lo llevan
    /// las ejecuciones que nacieron de un disparo programado; las pedidas por API fallan en el acto.
    /// </summary>
    public DateTimeOffset? DispatchDeadline { get; set; }

    public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;

    public ExecutionMode Mode { get; set; } = ExecutionMode.Attached;

    public int? ExitCode { get; set; }

    public string? StdOut { get; set; }

    public string? StdErr { get; set; }

    /// <summary>Motivo del fallo cuando no proviene del proceso (timeout, agente offline, checksum inválido).</summary>
    public string? ErrorMessage { get; set; }

    public DateTimeOffset QueuedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public bool IsFinished => Status is ExecutionStatus.Succeeded or ExecutionStatus.Failed or ExecutionStatus.TimedOut or ExecutionStatus.Canceled;
}
