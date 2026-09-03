using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Messages;
using Baion.Orchestrator.Messaging;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Persistence;
using Microsoft.Extensions.Logging;

namespace Baion.Orchestrator.Services.Implementations;

internal class ServerService(
    IRepository<Server> servers,
    IServerQueries queries,
    IAgentRegistry registry,
    IAgentCommandBus commandBus,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ServerService> logger) : IServerService
{
    public async Task<IReadOnlyList<ServerSummary>> GetAllAsync(CancellationToken cancellationToken) => await queries.ListAsync(cancellationToken);

    public async Task<Result<ServerDetail>> GetAsync(Guid serverId, CancellationToken cancellationToken)
    {
        var server = await servers.GetByIdAsync(serverId);

        if (server is null)
        {
            return Result<ServerDetail>.Failure(Error.NotFound("server.not_found", "El servidor no existe."));
        }

        var metrics = await queries.GetLastMetricsAsync(serverId, cancellationToken);

        return Result<ServerDetail>.Success(new ServerDetail(ToSummary(server), metrics));
    }

    public async Task<Result<PagedResult<MetricReading>>> GetMetricsAsync(Guid serverId, MetricsWindow window, int page, int pageSize, CancellationToken cancellationToken)
    {
        var server = await servers.GetByIdAsync(serverId);

        if (server is null)
        {
            return Result<PagedResult<MetricReading>>.Failure(Error.NotFound("server.not_found", "El servidor no existe."));
        }

        var historico = await queries.ListMetricsAsync(serverId, window, page, pageSize, cancellationToken);

        return Result<PagedResult<MetricReading>>.Success(historico);
    }

    /// <summary>
    /// El orden importa: primero se marca en base y solo después se corta. Al revés, el agente podría
    /// reconectar en el hueco y quedarse dentro con el servidor ya dado por desactivado.
    /// </summary>
    public async Task<Result<ServerSummary>> DisableAsync(Guid serverId, CancellationToken cancellationToken)
    {
        var server = await servers.GetByIdAsync(serverId);

        if (server is null)
        {
            return Result<ServerSummary>.Failure(Error.NotFound("server.not_found", "El servidor no existe."));
        }

        if (server.Status is not ServerStatus.Disabled)
        {
            server.Status = ServerStatus.Disabled;

            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Servidor {ServerId} desactivado", serverId);
        }

        // Se desconecta también cuando ya estaba desactivado: si el corte anterior falló, repetir la
        // operación es la única forma de volver a intentarlo.
        await DisconnectAsync(server, cancellationToken);

        return Result<ServerSummary>.Success(ToSummary(server));
    }

    public async Task<Result<ServerSummary>> EnableAsync(Guid serverId, CancellationToken cancellationToken)
    {
        var server = await servers.GetByIdAsync(serverId);

        if (server is null)
        {
            return Result<ServerSummary>.Failure(Error.NotFound("server.not_found", "El servidor no existe."));
        }

        if (server.Status is ServerStatus.Disabled)
        {
            // Queda desconectado, no en línea: el agente reintenta por su cuenta y es su saludo el que
            // vuelve a ponerlo en línea con la versión y la plataforma que traiga.
            server.Status = ServerStatus.Offline;
            server.OrchestratorInstanceId = null;
            server.ConnectedAt = null;

            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Servidor {ServerId} reactivado", serverId);
        }

        return Result<ServerSummary>.Success(ToSummary(server));
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken) => await queries.GetDashboardSummaryAsync(timeProvider.GetUtcNow().AddHours(-24), cancellationToken);

    /// <summary>
    /// Corta por los dos caminos posibles. El aviso va por el bus, que sabe llegar a la instancia que
    /// tenga el socket y le dice al agente por qué se queda fuera; el cierre solo puede hacerlo quien
    /// tiene el socket en su propio proceso, y si es este, no se espera a que el agente cuelgue.
    /// </summary>
    private async Task DisconnectAsync(Server server, CancellationToken cancellationToken)
    {
        var aviso = new ConnectionRejectedMessage("agent.server_disabled", "El servidor fue desactivado desde el panel.");
        await commandBus.TrySendAsync(server.Id, aviso, cancellationToken);

        if (!registry.TryGet(server.Id, out var connection))
        {
            return;
        }

        registry.Remove(connection);
        await connection.CloseAsync("El servidor fue desactivado.");
    }

    internal static ServerSummary ToSummary(Server server) => new(
        server.Id,
        server.Name,
        server.Hostname,
        server.Kind,
        server.Platform,
        server.Status,
        server.AgentVersion,
        server.RuntimeIdentifier,
        server.OrchestratorInstanceId,
        server.ConnectedAt,
        server.LastSeenAt,
        server.MaxConcurrentExecutions);
}
