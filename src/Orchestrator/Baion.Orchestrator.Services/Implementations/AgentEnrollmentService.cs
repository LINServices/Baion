using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Contracts.Messages;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Orchestrator.Services.Implementations;

internal class AgentEnrollmentService(IAgentRepository agents, ITenantContext tenantContext, IUnitOfWork unitOfWork, IOptions<OrchestratorOptions> options, TimeProvider timeProvider, ILogger<AgentEnrollmentService> logger) : IAgentEnrollmentService
{
    public async Task<Result<AgentCredentialContext>> ResolveCredentialsAsync(AgentCredentials credentials, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(credentials.AgentToken))
        {
            return await ResolveAgentTokenAsync(credentials.AgentToken, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(credentials.EnrollmentToken))
        {
            return await ResolveEnrollmentTokenAsync(credentials.EnrollmentToken, cancellationToken);
        }

        return InvalidCredentials;
    }

    public async Task<Result<AgentSession>> CompleteHandshakeAsync(AgentCredentialContext context, HelloMessage hello, CancellationToken cancellationToken)
    {
        if (hello.ProtocolVersion != BaionProtocol.Version)
        {
            return Result<AgentSession>.Failure(Error.Validation("agent.protocol_mismatch", $"El agente habla el protocolo {hello.ProtocolVersion} y esta instancia el {BaionProtocol.Version}."));
        }

        if (string.IsNullOrWhiteSpace(hello.MachineId))
        {
            return Result<AgentSession>.Failure(Error.Validation("agent.machine_id_required", "El saludo no trae identificador de máquina."));
        }

        tenantContext.SetTenant(context.TenantId);

        var now = timeProvider.GetUtcNow();
        var settings = options.Value;

        var server = context.ServerId is Guid knownServerId
            ? await agents.FindByIdAsync(context.TenantId, knownServerId, cancellationToken)
            : await agents.FindByMachineIdAsync(context.TenantId, hello.MachineId, cancellationToken);

        if (server is null && context.ServerId is not null)
        {
            // La credencial resolvió un servidor que ya no está: el agente tendría que volver a enrolarse.
            return Result<AgentSession>.Failure(Error.Unauthorized("agent.server_gone", "El servidor asociado a la credencial ya no existe."));
        }

        string? issuedToken = null;

        if (server is null)
        {
            issuedToken = AgentTokens.Generate();
            server = CreateServer(hello, AgentTokens.Hash(issuedToken), await ResolveNameAsync(context.TenantId, hello, cancellationToken));
            await agents.AddAsync(server);
            logger.LogInformation("Servidor {MachineId} enrolado en el tenant {TenantId}", hello.MachineId, context.TenantId);
        }
        else if (server.AgentTokenHash is null)
        {
            // Máquina ya conocida que llega con token de instalación: perdió su credencial y se le emite otra.
            issuedToken = AgentTokens.Generate();
            server.AgentTokenHash = AgentTokens.Hash(issuedToken);
        }

        ApplyHello(server, hello, now, settings.InstanceId);

        if (context.EnrollmentTokenId is Guid enrollmentTokenId)
        {
            await ConsumeEnrollmentTokenAsync(enrollmentTokenId, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AgentSession>.Success(new AgentSession(context.TenantId, server.Id, server.MaxConcurrentExecutions, settings.HeartbeatSeconds) { IssuedAgentToken = issuedToken });
    }

    public async Task TouchAsync(Guid tenantId, Guid serverId, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(tenantId);
        var server = await agents.FindByIdAsync(tenantId, serverId, cancellationToken);

        if (server is null)
        {
            return;
        }

        server.LastSeenAt = timeProvider.GetUtcNow();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkDisconnectedAsync(Guid tenantId, Guid serverId, CancellationToken cancellationToken)
    {
        // La baja va en una sola escritura condicional y no toca el estado de un servidor desactivado:
        // lo habitual es que el socket muera justamente porque acaban de desactivarlo, y devolverlo a
        // Offline aquí lo readmitiría sin que nadie lo haya reactivado.
        await agents.MarkDisconnectedAsync(tenantId, serverId, timeProvider.GetUtcNow(), cancellationToken);
        logger.LogInformation("Servidor {ServerId} marcado como desconectado", serverId);
    }

    private async Task<Result<AgentCredentialContext>> ResolveAgentTokenAsync(string agentToken, CancellationToken cancellationToken)
    {
        var server = await agents.FindByAgentTokenAsync(AgentTokens.Hash(agentToken), cancellationToken);

        if (server is null)
        {
            logger.LogWarning("Conexión de agente rechazada: credencial desconocida");
            return InvalidCredentials;
        }

        if (server.Status is ServerStatus.Disabled)
        {
            return Result<AgentCredentialContext>.Failure(Error.Forbidden("agent.server_disabled", "El servidor está deshabilitado."));
        }

        return Result<AgentCredentialContext>.Success(new AgentCredentialContext(server.TenantId, server.Id, null));
    }

    private async Task<Result<AgentCredentialContext>> ResolveEnrollmentTokenAsync(string enrollmentToken, CancellationToken cancellationToken)
    {
        var token = await agents.FindEnrollmentTokenAsync(AgentTokens.Hash(enrollmentToken), cancellationToken);

        if (token is null || !token.IsUsable(timeProvider.GetUtcNow()))
        {
            logger.LogWarning("Enrolamiento rechazado: token inexistente, caducado, revocado o agotado");
            return InvalidCredentials;
        }

        if (!token.Tenant.IsActive)
        {
            return Result<AgentCredentialContext>.Failure(Error.Forbidden("agent.tenant_inactive", "El tenant está inactivo."));
        }

        return Result<AgentCredentialContext>.Success(new AgentCredentialContext(token.TenantId, null, token.Id));
    }

    private async Task ConsumeEnrollmentTokenAsync(Guid enrollmentTokenId, CancellationToken cancellationToken)
    {
        var token = await agents.FindEnrollmentTokenByIdAsync(enrollmentTokenId, cancellationToken);

        if (token is not null)
        {
            token.UseCount++;
        }
    }

    /// <summary>
    /// El nombre sale del hostname porque es lo que reconoce una persona, pero dos máquinas pueden
    /// compartirlo: contenedores, VMs clonadas de una plantilla, o la misma máquina reenrolándose tras
    /// perder su estado. Como el nombre es único por tenant, se desambigua con el identificador de máquina.
    /// </summary>
    private async Task<string> ResolveNameAsync(Guid tenantId, HelloMessage hello, CancellationToken cancellationToken)
    {
        var candidato = Truncate(hello.Hostname);

        if (!await agents.NameExistsAsync(tenantId, candidato, cancellationToken))
        {
            return candidato;
        }

        var conMaquina = Truncate($"{hello.Hostname}-{ShortId(hello.MachineId)}");

        return await agents.NameExistsAsync(tenantId, conMaquina, cancellationToken)
            ? Truncate($"{hello.Hostname}-{Guid.NewGuid():N}")
            : conMaquina;
    }

    private static string ShortId(string machineId) => machineId.Length <= MachineIdSuffixLength ? machineId : machineId[..MachineIdSuffixLength];

    private static string Truncate(string value) => value.Length <= MaxNameLength ? value : value[..MaxNameLength];

    private static Server CreateServer(HelloMessage hello, string agentTokenHash, string name) => new()
    {
        Name = name,
        Hostname = hello.Hostname,
        MachineId = hello.MachineId,
        AgentTokenHash = agentTokenHash,
        Kind = ServerKind.Vps,
        Platform = hello.Platform
    };

    private static void ApplyHello(Server server, HelloMessage hello, DateTimeOffset now, string instanceId)
    {
        server.Hostname = hello.Hostname;
        server.Platform = hello.Platform;
        server.AgentVersion = hello.AgentVersion;
        server.RuntimeIdentifier = hello.RuntimeIdentifier;
        server.Status = ServerStatus.Online;
        server.OrchestratorInstanceId = instanceId;
        server.ConnectedAt = now;
        server.LastSeenAt = now;
    }

    private const int MaxNameLength = 200;

    private const int MachineIdSuffixLength = 8;

    // Un único error para credencial desconocida, caducada, revocada o agotada: no se revela cuál de las cuatro.
    private static readonly Result<AgentCredentialContext> InvalidCredentials = Result<AgentCredentialContext>.Failure(Error.Unauthorized("agent.invalid_credentials", "Las credenciales del agente no son válidas."));
}
