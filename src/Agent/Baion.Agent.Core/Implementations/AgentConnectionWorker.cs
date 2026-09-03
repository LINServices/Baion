using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Contracts.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Agent.Core.Implementations;

/// <summary>
/// Mantiene abierta la conexión con el orquestador. El agente es siempre quien marca: solo necesita salida
/// a internet, y reconecta con retroceso exponencial más jitter cuando el canal se cae.
/// </summary>
internal class AgentConnectionWorker(IAgentStateStore stateStore, IPlatformInfoProvider platform, IReconnectPolicy reconnectPolicy, IEnumerable<IServerMessageHandler> handlers, OrchestratorChannel orchestratorChannel, IOptions<AgentOptions> options, ILogger<AgentConnectionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var establecida = await RunSessionAsync(stoppingToken);

                if (establecida)
                {
                    attempt = 0;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning("La sesión con el orquestador terminó: {Motivo}", exception.Message);
            }

            attempt++;
            var delay = reconnectPolicy.GetDelay(attempt);
            logger.LogInformation("Reintentando la conexión en {Segundos:F1} s (intento {Intento})", delay.TotalSeconds, attempt);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>Abre una sesión completa y la mantiene. Devuelve true si llegó a establecerse.</summary>
    private async Task<bool> RunSessionAsync(CancellationToken stoppingToken)
    {
        var state = await stateStore.LoadAsync(stoppingToken);

        if (!state.IsEnrolled && string.IsNullOrWhiteSpace(options.Value.EnrollmentToken))
        {
            logger.LogError("El agente no está enrolado y no hay token de instalación configurado en {Section}:EnrollmentToken", AgentOptions.SectionName);
            return false;
        }

        using var socket = new ClientWebSocket();
        ApplyCredentials(socket, state);

        var uri = BuildSocketUri(options.Value.OrchestratorUri);
        logger.LogInformation("Conectando con el orquestador en {Uri}", uri);

        await socket.ConnectAsync(uri, stoppingToken);

        using var channel = new BaionMessageChannel(socket);
        await channel.SendAsync<AgentToServerMessage>(BuildHello(state), stoppingToken);

        var respuesta = await channel.ReceiveAsync<ServerToAgentMessage>(stoppingToken);

        if (respuesta is ConnectionRejectedMessage rejected)
        {
            await HandleRejectionAsync(state, rejected, stoppingToken);
            return false;
        }

        if (respuesta is not WelcomeMessage welcome)
        {
            logger.LogError("El orquestador respondió al saludo con {Respuesta} en lugar de la bienvenida", respuesta?.GetType().Name ?? "un cierre");
            return false;
        }

        await PersistEnrollmentAsync(state, welcome, stoppingToken);
        logger.LogInformation("Agente conectado como servidor {ServerId} del tenant {TenantId}", welcome.ServerId, welcome.TenantId);

        using var session = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        // A partir de aquí el resto del agente puede enviar por este canal.
        orchestratorChannel.Attach(channel, new AgentSessionInfo(welcome.ServerId, welcome.TenantId, welcome.MaxConcurrentExecutions));

        try
        {
            var heartbeat = SendHeartbeatsAsync(channel, welcome.HeartbeatSeconds, session.Token);
            var pump = PumpAsync(channel, session.Token);

            await Task.WhenAny(heartbeat, pump);
            await session.CancelAsync();

            // Se esperan las dos para que ninguna siga escribiendo sobre un socket ya cerrado.
            await Task.WhenAll(Swallow(heartbeat), Swallow(pump));
        }
        finally
        {
            orchestratorChannel.Detach(channel);
        }

        return true;
    }

    private async Task PumpAsync(BaionMessageChannel channel, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await channel.ReceiveAsync<ServerToAgentMessage>(cancellationToken);

            if (message is null)
            {
                logger.LogInformation("El orquestador cerró la conexión");
                return;
            }

            if (message is ConnectionRejectedMessage expulsion)
            {
                // El rechazo también puede llegar a mitad de sesión, cuando el servidor se desactiva
                // desde el panel. La credencial sigue valiendo, así que se conserva y se reintenta.
                logger.LogError("El orquestador expulsó al agente: {Code} — {Reason}", expulsion.Code, expulsion.Reason);
                return;
            }

            var handler = handlers.FirstOrDefault(candidate => candidate.CanHandle(message));

            if (handler is null)
            {
                logger.LogWarning("Mensaje {MessageType} sin manejador registrado en este agente", message.GetType().Name);
                continue;
            }

            await handler.HandleAsync(message, cancellationToken);
        }
    }

    private async Task SendHeartbeatsAsync(BaionMessageChannel channel, int heartbeatSeconds, CancellationToken cancellationToken)
    {
        var period = TimeSpan.FromSeconds(Math.Max(heartbeatSeconds, 1));

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(period, cancellationToken);
            await channel.SendAsync<AgentToServerMessage>(new HeartbeatMessage(RunningExecutions: 0), cancellationToken);
        }
    }

    /// <summary>Si el orquestador ya no reconoce la credencial, se descarta para volver a enrolarse.</summary>
    private async Task HandleRejectionAsync(AgentState state, ConnectionRejectedMessage rejected, CancellationToken cancellationToken)
    {
        logger.LogError("El orquestador rechazó la conexión: {Code} — {Reason}", rejected.Code, rejected.Reason);

        if (state.IsEnrolled && CredentialRejectionCodes.Contains(rejected.Code))
        {
            logger.LogWarning("Se descarta la credencial guardada; el próximo intento usará el token de instalación");
            await stateStore.SaveAsync(state with { ServerId = null, AgentToken = null }, cancellationToken);
        }
    }

    private async Task PersistEnrollmentAsync(AgentState state, WelcomeMessage welcome, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(welcome.AgentToken) && state.ServerId == welcome.ServerId)
        {
            return;
        }

        await stateStore.SaveAsync(state with { ServerId = welcome.ServerId, AgentToken = welcome.AgentToken ?? state.AgentToken }, cancellationToken);
    }

    private void ApplyCredentials(ClientWebSocket socket, AgentState state)
    {
        socket.Options.SetRequestHeader(BaionProtocol.ProtocolVersionHeader, BaionProtocol.Version);

        if (state.IsEnrolled)
        {
            socket.Options.SetRequestHeader(BaionProtocol.AgentTokenHeader, state.AgentToken);
            return;
        }

        socket.Options.SetRequestHeader(BaionProtocol.EnrollmentTokenHeader, options.Value.EnrollmentToken);
    }

    private HelloMessage BuildHello(AgentState state) => new(BaionProtocol.Version, platform.Platform, platform.RuntimeIdentifier, platform.AgentVersion, platform.Hostname, state.MachineId, platform.CoreCount, platform.TotalMemoryBytes);

    private static Uri BuildSocketUri(string orchestratorUri) => new(new Uri(orchestratorUri.TrimEnd('/') + "/"), BaionProtocol.WebSocketPath.TrimStart('/'));

    private static async Task Swallow(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception)
        {
            // El motivo real ya se registró en quien inició la tarea; aquí solo se drena.
        }
    }

    private static readonly HashSet<string> CredentialRejectionCodes = new(StringComparer.Ordinal) { "agent.invalid_credentials", "agent.server_gone" };
}
