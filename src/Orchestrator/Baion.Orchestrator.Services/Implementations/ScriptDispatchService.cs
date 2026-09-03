using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Contracts.Enums;
using Baion.Contracts.Messages;
using Baion.Orchestrator.Messaging;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Persistence;
using Microsoft.Extensions.Logging;

namespace Baion.Orchestrator.Services.Implementations;

internal class ScriptDispatchService(IRepository<Script> scripts, IRepository<Server> servers, IRepository<ScriptExecution> executions, IScriptExecutionRepository executionRepository, IScriptQueries queries, IAgentCommandBus commandBus, IUnitOfWork unitOfWork, TimeProvider timeProvider, ILogger<ScriptDispatchService> logger) : IScriptDispatchService
{
    public async Task<Result<ScriptExecutionDispatched>> DispatchAsync(DispatchScriptRequest request, CancellationToken cancellationToken)
    {
        if (request.TimeoutSeconds is <= 0)
        {
            return Result<ScriptExecutionDispatched>.Failure(Error.Validation("execution.timeout_invalid", "El timeout debe ser mayor que cero."));
        }

        var script = await scripts.GetByIdAsync(request.ScriptId);

        if (script is null || !script.IsActive)
        {
            return Result<ScriptExecutionDispatched>.Failure(Error.NotFound("script.not_found", "El script no existe o está inactivo."));
        }

        var server = await servers.GetByIdAsync(request.ServerId);

        if (server is null)
        {
            return Result<ScriptExecutionDispatched>.Failure(Error.NotFound("server.not_found", "El servidor no existe."));
        }

        var prepared = new PreparedDispatch(script, server, request.Mode, request.TimeoutSeconds ?? script.DefaultTimeoutSeconds, request.WorkingDirectory, request.EnvironmentVariables);

        return await SendAsync(prepared, cancellationToken);
    }

    public async Task<Result<ScriptExecutionDispatched>> DispatchScheduledAsync(ScheduledDispatch request, CancellationToken cancellationToken)
    {
        var script = await scripts.GetByIdAsync(request.ScriptId);

        if (script is null || !script.IsActive)
        {
            return Result<ScriptExecutionDispatched>.Failure(Error.NotFound("script.not_found", "El script de la tarea no existe o está inactivo."));
        }

        var server = await servers.GetByIdAsync(request.ServerId);

        if (server is null)
        {
            return Result<ScriptExecutionDispatched>.Failure(Error.NotFound("server.not_found", "El servidor no existe."));
        }

        var prepared = new PreparedDispatch(script, server, request.Mode, script.DefaultTimeoutSeconds, null, null)
        {
            ScheduledTaskId = request.ScheduledTaskId,
            DispatchDeadline = request.DispatchDeadline
        };

        return await SendAsync(prepared, cancellationToken);
    }

    public async Task<Result<ScriptExecutionDispatched>> DispatchChainStepAsync(ChainStepDispatch request, CancellationToken cancellationToken)
    {
        var script = await scripts.GetByIdAsync(request.ScriptId);

        if (script is null || !script.IsActive)
        {
            return Result<ScriptExecutionDispatched>.Failure(Error.NotFound("script.not_found", "El script del paso no existe o está inactivo."));
        }

        var server = await servers.GetByIdAsync(request.ServerId);

        if (server is null)
        {
            return Result<ScriptExecutionDispatched>.Failure(Error.NotFound("server.not_found", "El servidor no existe."));
        }

        // Los pasos van siempre Attached: sin código de salida no habría con qué evaluar la política de fallo.
        var prepared = new PreparedDispatch(script, server, ExecutionMode.Attached, request.TimeoutSeconds, null, null)
        {
            ChainRunId = request.ChainRunId,
            ScriptChainStepId = request.ScriptChainStepId,
            ScheduledTaskId = request.ScheduledTaskId,
            DispatchDeadline = request.DispatchDeadline
        };

        return await SendAsync(prepared, cancellationToken);
    }

    public async Task<Result<ScriptExecutionDetail>> GetAsync(Guid executionId, CancellationToken cancellationToken)
    {
        var execution = await executionRepository.GetWithNamesAsync(executionId, cancellationToken);

        return execution is null
            ? Result<ScriptExecutionDetail>.Failure(Error.NotFound("execution.not_found", "La ejecución no existe."))
            : Result<ScriptExecutionDetail>.Success(ToDetail(execution));
    }

    public async Task<PagedResult<ScriptExecutionListItem>> ListAsync(ExecutionFilter filter, int page, int pageSize, CancellationToken cancellationToken) => await queries.ListExecutionsAsync(filter, page, pageSize, cancellationToken);

    public async Task ApplyAsync(ScriptExecutionEvent notification, CancellationToken cancellationToken)
    {
        switch (notification)
        {
            case ScriptOutputEvent output:
                await executionRepository.AppendOutputAsync(output.ExecutionId, output.Stream, output.Content, cancellationToken);
                return;

            case ScriptStartEvent started:
                await ApplyStartAsync(started, cancellationToken);
                return;

            case ScriptCompletionEvent completed:
                await ApplyCompletionAsync(completed, cancellationToken);
                return;
        }
    }

    private async Task<Result<ScriptExecutionDispatched>> SendAsync(PreparedDispatch prepared, CancellationToken cancellationToken)
    {
        if (prepared.Server.Status is ServerStatus.Disabled)
        {
            return Result<ScriptExecutionDispatched>.Failure(Error.Conflict("server.disabled", "El servidor está desactivado y no admite ejecuciones."));
        }

        // Se comprueba aquí para fallar antes de crear la fila; el agente lo vuelve a comprobar por su cuenta.
        if (!ScriptRuntimeCompatibility.IsSupported(prepared.Script.Runtime, prepared.Server.Platform))
        {
            return Result<ScriptExecutionDispatched>.Failure(Error.Validation("execution.runtime_incompatible", $"Un script {prepared.Script.Runtime} no puede ejecutarse en un servidor {prepared.Server.Platform}."));
        }

        var execution = CreateExecution(prepared);
        await executions.AddAsync(execution);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var order = new ExecuteScriptMessage(execution.Id, prepared.Script.Content, prepared.Script.Checksum, prepared.Script.Runtime, prepared.Mode, prepared.TimeoutSeconds, prepared.WorkingDirectory, prepared.EnvironmentVariables);

        // El bus resuelve dónde vive el socket: si es aquí lo entrega en el acto, y si es otra instancia
        // lo publica con la clave del servidor. Esta capa no necesita saber cuál de las dos ocurrió.
        if (!await commandBus.TrySendAsync(prepared.Server.Id, order, cancellationToken))
        {
            return await HandleUndeliveredAsync(execution, prepared, cancellationToken);
        }

        execution.Status = ExecutionStatus.Dispatched;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Ejecución {ExecutionId} despachada al servidor {ServerId} en modo {Mode}", execution.Id, prepared.Server.Id, prepared.Mode);

        return Result<ScriptExecutionDispatched>.Success(new ScriptExecutionDispatched(execution.Id, execution.Status));
    }

    /// <summary>
    /// Una orden pedida por API falla en el acto y no deja rastro: quien la lanzó está esperando la
    /// respuesta y no llegó a salir nada. Una nacida de una tarea programada, en cambio, queda en espera
    /// hasta su plazo, porque la desconexión del agente suele ser un reinicio o un corte pasajero y no un
    /// motivo para perder el disparo.
    /// </summary>
    private async Task<Result<ScriptExecutionDispatched>> HandleUndeliveredAsync(ScriptExecution execution, PreparedDispatch prepared, CancellationToken cancellationToken)
    {
        if (prepared.DispatchDeadline is null)
        {
            executions.Remove(execution);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ScriptExecutionDispatched>.Failure(Error.Conflict("agent.not_connected", "El agente del servidor no está conectado a ninguna instancia."));
        }

        execution.ErrorMessage = "Esperando a que el agente se conecte.";
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Ejecución {ExecutionId} en espera: el agente del servidor {ServerId} no está conectado, plazo hasta {Plazo}", execution.Id, prepared.Server.Id, prepared.DispatchDeadline);

        return Result<ScriptExecutionDispatched>.Success(new ScriptExecutionDispatched(execution.Id, ExecutionStatus.Pending));
    }

    public async Task RetryPendingAsync(Guid executionId, CancellationToken cancellationToken)
    {
        var execution = await executionRepository.GetByIdAsync(executionId, cancellationToken);

        if (execution is not { Status: ExecutionStatus.Pending, DispatchDeadline: DateTimeOffset deadline })
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        if (now > deadline)
        {
            execution.Status = ExecutionStatus.Failed;
            execution.ErrorMessage = "El agente no volvió a conectarse dentro del margen configurado.";
            execution.CompletedAt = now;

            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Ejecución {ExecutionId} descartada: el agente no volvió antes de {Plazo}", execution.Id, deadline);

            return;
        }

        var script = await scripts.GetByIdAsync(execution.ScriptId);

        if (script is null)
        {
            execution.Status = ExecutionStatus.Failed;
            execution.ErrorMessage = "El script ya no existe.";
            execution.CompletedAt = now;

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var order = new ExecuteScriptMessage(execution.Id, script.Content, script.Checksum, script.Runtime, execution.Mode, script.DefaultTimeoutSeconds, null, null);

        // Se deja en espera si sigue sin haber a quién entregarla: el plazo decidirá si merece otro
        // intento o se da por perdida.
        if (!await commandBus.TrySendAsync(execution.ServerId, order, cancellationToken))
        {
            return;
        }

        execution.Status = ExecutionStatus.Dispatched;
        execution.ErrorMessage = null;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Ejecución {ExecutionId} entregada tras volver el agente del servidor {ServerId}", execution.Id, execution.ServerId);
    }

    private async Task ApplyStartAsync(ScriptStartEvent notification, CancellationToken cancellationToken)
    {
        var execution = await executionRepository.GetByIdAsync(notification.ExecutionId, cancellationToken);

        if (execution is null || execution.IsFinished)
        {
            return;
        }

        execution.Status = ExecutionStatus.Running;
        execution.StartedAt = notification.StartedAt;

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyCompletionAsync(ScriptCompletionEvent notification, CancellationToken cancellationToken)
    {
        var execution = await executionRepository.GetByIdAsync(notification.ExecutionId, cancellationToken);

        if (execution is null)
        {
            logger.LogWarning("Llegó el desenlace de la ejecución desconocida {ExecutionId}", notification.ExecutionId);
            return;
        }

        execution.Status = notification.Status;
        execution.ExitCode = notification.ExitCode;
        execution.ErrorMessage = notification.ErrorMessage;
        execution.CompletedAt = notification.CompletedAt;
        execution.StartedAt ??= notification.CompletedAt;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Ejecución {ExecutionId} terminada con estado {Status} y código {ExitCode}", execution.Id, execution.Status, execution.ExitCode);
    }

    // std_out y std_err arrancan vacíos, no nulos: .WRITE de SQL Server no puede añadir sobre un NULL.
    private ScriptExecution CreateExecution(PreparedDispatch prepared) => new()
    {
        ServerId = prepared.Server.Id,
        ScriptId = prepared.Script.Id,
        ChainRunId = prepared.ChainRunId,
        ScriptChainStepId = prepared.ScriptChainStepId,
        ScheduledTaskId = prepared.ScheduledTaskId,
        DispatchDeadline = prepared.DispatchDeadline,
        Mode = prepared.Mode,
        Status = ExecutionStatus.Pending,
        QueuedAt = timeProvider.GetUtcNow(),
        StdOut = string.Empty,
        StdErr = string.Empty
    };

    private static ScriptExecutionDetail ToDetail(ScriptExecution execution) => new(
        execution.Id,
        execution.ServerId,
        execution.Server.Name,
        execution.ScriptId,
        execution.Script.Name,
        execution.Status,
        execution.Mode,
        execution.ExitCode,
        execution.StdOut,
        execution.StdErr,
        execution.ErrorMessage,
        execution.QueuedAt,
        execution.StartedAt,
        execution.CompletedAt);

    /// <summary>Todo lo ya resuelto y validado que necesita el envío, venga de una orden suelta, de una cadena o de una tarea.</summary>
    private record PreparedDispatch(Script Script, Server Server, ExecutionMode Mode, int TimeoutSeconds, string? WorkingDirectory, IReadOnlyDictionary<string, string>? EnvironmentVariables)
    {
        public Guid? ChainRunId { get; init; }

        public Guid? ScriptChainStepId { get; init; }

        public Guid? ScheduledTaskId { get; init; }

        public DateTimeOffset? DispatchDeadline { get; init; }
    }
}
