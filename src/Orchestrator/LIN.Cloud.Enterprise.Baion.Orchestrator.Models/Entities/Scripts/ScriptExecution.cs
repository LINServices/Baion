using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

public class ScriptExecution : TenantEntity
{
    public Guid ServerId { get; set; }

    public Server Server { get; set; } = null!;

    public Guid ScriptId { get; set; }

    public Script Script { get; set; } = null!;

    // Paso de cadena que originó la ejecución, cuando no es una ejecución suelta.
    public Guid? ScriptChainStepId { get; set; }

    public ScriptChainStep? ScriptChainStep { get; set; }

    // Correlaciona las ejecuciones de un mismo recorrido de cadena, incluso si hay varios en paralelo.
    public Guid? ChainRunId { get; set; }

    // Tarea programada que originó la ejecución, cuando no la pidió una persona.
    public Guid? ScheduledTaskId { get; set; }

    public ScheduledTask? ScheduledTask { get; set; }

    // Instante hasta el que se sigue intentando entregar la orden a un agente desconectado. Solo lo llevan
    // las ejecuciones que nacieron de un disparo programado; las pedidas por API fallan en el acto.
    public DateTimeOffset? DispatchDeadline { get; set; }

    // Lote de ejecución sobre un grupo que originó la ejecución, cuando la pidió un lanzamiento por grupo.
    public Guid? BatchId { get; set; }

    public ExecutionBatch? Batch { get; set; }

    public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;

    public ExecutionMode Mode { get; set; } = ExecutionMode.Attached;

    public int? ExitCode { get; set; }

    public string? StdOut { get; set; }

    public string? StdErr { get; set; }

    // Motivo del fallo cuando no proviene del proceso (timeout, agente offline, checksum inválido).
    public string? ErrorMessage { get; set; }

    public DateTimeOffset QueuedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public bool IsFinished => Status is ExecutionStatus.Succeeded or ExecutionStatus.Failed or ExecutionStatus.TimedOut or ExecutionStatus.Canceled;
}
