using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Contracts.Messages;
using Baion.Contracts.Services;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Persistence;
using Microsoft.Extensions.Options;

namespace Baion.Orchestrator.Services.Implementations;

internal sealed class ServiceInspectionService(IRepository<Server> servers, IAgentQueryDispatcher dispatcher, IOptions<AgentQueryOptions> options) : IServiceInspectionService
{
    public async Task<Result<IReadOnlyList<ServiceSummary>>> ListAsync(Guid serverId, string? nameFilter, CancellationToken cancellationToken)
    {
        var server = await servers.GetByIdAsync(serverId);

        if (server is null)
        {
            return Result<IReadOnlyList<ServiceSummary>>.Failure(ServerNotFound);
        }

        var requestId = Guid.CreateVersion7();
        var respuesta = await dispatcher.QueryAsync(serverId, requestId, new ListServicesRequestMessage(requestId, Normalize(nameFilter)), cancellationToken);

        return Interpret<ServicesListedMessage, IReadOnlyList<ServiceSummary>>(respuesta, mensaje => mensaje.Services);
    }

    public async Task<Result<ServiceDetail>> DescribeAsync(Guid serverId, string serviceId, CancellationToken cancellationToken)
    {
        var validacion = ValidateServiceId(serviceId);

        if (validacion is not null)
        {
            return Result<ServiceDetail>.Failure(validacion);
        }

        var server = await servers.GetByIdAsync(serverId);

        if (server is null)
        {
            return Result<ServiceDetail>.Failure(ServerNotFound);
        }

        var requestId = Guid.CreateVersion7();
        var respuesta = await dispatcher.QueryAsync(serverId, requestId, new DescribeServiceRequestMessage(requestId, serviceId), cancellationToken);

        return Interpret<ServiceDescribedMessage, ServiceDetail>(respuesta, mensaje => mensaje.Service);
    }

    public async Task<Result<ServiceLogPage>> GetLogsAsync(Guid serverId, string serviceId, int? maxLines, DateTimeOffset? since, CancellationToken cancellationToken)
    {
        var validacion = ValidateServiceId(serviceId);

        if (validacion is not null)
        {
            return Result<ServiceLogPage>.Failure(validacion);
        }

        var server = await servers.GetByIdAsync(serverId);

        if (server is null)
        {
            return Result<ServiceLogPage>.Failure(ServerNotFound);
        }

        var lineas = Math.Clamp(maxLines ?? options.Value.DefaultLogLines, 1, options.Value.MaxLogLines);
        var requestId = Guid.CreateVersion7();
        var respuesta = await dispatcher.QueryAsync(serverId, requestId, new ServiceLogsRequestMessage(requestId, serviceId, lineas, since), cancellationToken);

        return Interpret<ServiceLogsMessage, ServiceLogPage>(respuesta, mensaje => new ServiceLogPage(mensaje.Lines, mensaje.Truncated));
    }

    public async Task<Result<ServiceDetail>> ControlAsync(Guid serverId, string serviceId, ServiceControlAction action, CancellationToken cancellationToken)
    {
        var validacion = ValidateServiceId(serviceId);

        if (validacion is not null)
        {
            return Result<ServiceDetail>.Failure(validacion);
        }

        if (!Enum.IsDefined(action))
        {
            return Result<ServiceDetail>.Failure(Error.Validation("service.action_invalid", "La acción de control no es válida."));
        }

        var server = await servers.GetByIdAsync(serverId);

        if (server is null)
        {
            return Result<ServiceDetail>.Failure(ServerNotFound);
        }

        if (server.Status is ServerStatus.Disabled)
        {
            return Result<ServiceDetail>.Failure(Error.Conflict("server.disabled", "El servidor está desactivado y no admite acciones de control."));
        }

        var requestId = Guid.CreateVersion7();
        var respuesta = await dispatcher.QueryAsync(serverId, requestId, new ControlServiceRequestMessage(requestId, serviceId, action), cancellationToken);

        return Interpret<ServiceControlledMessage, ServiceDetail>(respuesta, mensaje => mensaje.Service);
    }

    /// <summary>
    /// Un fallo de transporte se propaga tal cual; una respuesta del tipo esperado se proyecta; y un
    /// <see cref="ServiceQueryFailedMessage"/> se traduce al <see cref="Error"/> que le corresponde.
    /// </summary>
    private static Result<TValue> Interpret<TReply, TValue>(Result<AgentToServerMessage> respuesta, Func<TReply, TValue> proyectar)
        where TReply : AgentToServerMessage
    {
        if (respuesta.IsFailure)
        {
            return Result<TValue>.Failure(respuesta.Error!);
        }

        return respuesta.Value switch
        {
            TReply esperada => Result<TValue>.Success(proyectar(esperada)),
            ServiceQueryFailedMessage fallo => Result<TValue>.Failure(MapFailure(fallo)),
            _ => Result<TValue>.Failure(Error.Unexpected("agent.query_unexpected", "El agente respondió con un mensaje que no correspondía a la petición."))
        };
    }

    private static Error MapFailure(ServiceQueryFailedMessage fallo) => fallo.Code switch
    {
        ServiceQueryFailedMessage.NotFoundCode => Error.NotFound("service.not_found", fallo.Message),
        ServiceQueryFailedMessage.ForbiddenCode => Error.Forbidden("service.forbidden", fallo.Message),
        _ => Error.Unexpected("service.tool_failed", fallo.Message)
    };

    private static Error? ValidateServiceId(string serviceId) =>
        string.IsNullOrWhiteSpace(serviceId) ? Error.Validation("service.id_required", "Falta el identificador del servicio.") : null;

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static readonly Error ServerNotFound = Error.NotFound("server.not_found", "El servidor no existe.");
}
