using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Contracts;
using LIN.Cloud.Enterprise.Baion.Contracts.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LIN.Cloud.Enterprise.Baion.Agent.Core.Implementations;

// El agente es siempre quien marca: solo necesita salida a internet, y reconecta con
// retroceso exponencial más jitter cuando el canal se cae.
internal class AgentConnectionWorker(IAgentStateStore stateStore, IPlatformInfoProvider platform, IReconnectPolicy reconnectPolicy, IEnumerable<IServerMessageHandler> handlers, OrchestratorChannel orchestratorChannel, IOptions<AgentOptions> options, ILogger<AgentConnectionWorker> logger) : BackgroundService
{
    private static readonly HashSet<string> CredentialRejectionCodes = new(StringComparer.Ordinal) { "agent.invalid_credentials", "agent.server_gone" };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            attempt = await TryRunSessionAsync(stoppingToken) ? 1 : attempt + 1;
            await WaitBeforeRetryAsync(attempt, stoppingToken);
        }
    }

    private async Task WaitBeforeRetryAsync(int attempt, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
            return;

        var delay = reconnectPolicy.GetDelay(attempt);
        logger.LogInformation("Reintentando la conexión en {Segundos:F1} s (intento {Intento})", delay.TotalSeconds, attempt);

        await DelayAsync(delay, stoppingToken);
    }

    private async Task<bool> TryRunSessionAsync(CancellationToken stoppingToken)
    {
        try
        {
            return await RunSessionAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception)
        {
            logger.LogWarning("La sesión con el orquestador terminó: {Motivo}", exception.Message);
            return false;
        }
    }

    private static async Task DelayAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // La cancelación solo interrumpe la espera; el bucle principal decide si sigue.
        }
    }

    // Devuelve true si la sesión llegó a establecerse.
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

        await RunConnectedAsync(channel, welcome, stoppingToken);

        return true;
    }

    private async Task RunConnectedAsync(BaionMessageChannel channel, WelcomeMessage welcome, CancellationToken stoppingToken)
    {
        using var session = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

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
    }

    private async Task PumpAsync(BaionMessageChannel channel, CancellationToken cancellationToken)
    {
        var continuar = true;

        while (continuar && !cancellationToken.IsCancellationRequested)
            continuar = await ReceiveAndHandleAsync(channel, cancellationToken);
    }

    // Devuelve false cuando la sesión debe terminar (cierre o expulsión).
    private async Task<bool> ReceiveAndHandleAsync(BaionMessageChannel channel, CancellationToken cancellationToken)
    {
        var message = await channel.ReceiveAsync<ServerToAgentMessage>(cancellationToken);

        if (message is null)
        {
            logger.LogInformation("El orquestador cerró la conexión");
            return false;
        }

        if (message is ConnectionRejectedMessage expulsion)
        {
            // El rechazo también puede llegar a mitad de sesión, cuando el servidor se desactiva
            // desde el panel. La credencial sigue valiendo, así que se conserva y se reintenta.
            logger.LogError("El orquestador expulsó al agente: {Code} — {Reason}", expulsion.Code, expulsion.Reason);
            return false;
        }

        var handler = handlers.FirstOrDefault(candidate => candidate.CanHandle(message));

        if (handler is null)
            logger.LogWarning("Mensaje {MessageType} sin manejador registrado en este agente", message.GetType().Name);
        else
            await handler.HandleAsync(message, cancellationToken);

        return true;
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

    // Si el orquestador ya no reconoce la credencial, se descarta para volver a enrolarse.
    private async Task HandleRejectionAsync(AgentState state, ConnectionRejectedMessage rejected, CancellationToken cancellationToken)
    {
        logger.LogError("El orquestador rechazó la conexión: {Code} — {Reason}", rejected.Code, rejected.Reason);

        if (!state.IsEnrolled || !CredentialRejectionCodes.Contains(rejected.Code))
            return;

        logger.LogWarning("Se descarta la credencial guardada; el próximo intento usará el token de instalación");
        await stateStore.SaveAsync(state with { ServerId = null, AgentToken = null }, cancellationToken);
    }

    private async Task PersistEnrollmentAsync(AgentState state, WelcomeMessage welcome, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(welcome.AgentToken) && state.ServerId == welcome.ServerId)
            return;

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

    private HelloMessage BuildHello(AgentState state)
    {
        return new HelloMessage(BaionProtocol.Version, platform.Platform, platform.RuntimeIdentifier, platform.AgentVersion, platform.Hostname, state.MachineId, platform.CoreCount, platform.TotalMemoryBytes);
    }

    private static Uri BuildSocketUri(string orchestratorUri)
    {
        return new Uri(new Uri($"{orchestratorUri.TrimEnd('/')}/"), BaionProtocol.WebSocketPath.TrimStart('/'));
    }

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
}
