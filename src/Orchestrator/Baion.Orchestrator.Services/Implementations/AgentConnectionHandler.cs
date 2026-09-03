using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Contracts.Messages;
using Baion.Orchestrator.Messaging;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Orchestrator.Services.Implementations;

/// <summary>
/// Conduce un socket de agente de principio a fin. Cada operación contra la base abre su propio scope:
/// la conexión vive horas y no puede quedarse con un <c>DbContext</c> abierto todo ese tiempo.
/// </summary>
internal class AgentConnectionHandler(IServiceScopeFactory scopeFactory, IAgentRegistry registry, IMetricIngestQueue metricQueue, IScriptEventQueue scriptQueue, IAgentCommandSubscription subscription, IAgentPresenceBus presence, IOptions<OrchestratorOptions> options, TimeProvider timeProvider, ILogger<AgentConnectionHandler> logger) : IAgentConnectionHandler
{
    public async Task HandleAsync(WebSocket socket, AgentCredentialContext credentials, CancellationToken cancellationToken)
    {
        using var channel = new BaionMessageChannel(socket);

        var hello = await ReceiveHelloAsync(channel, cancellationToken);

        if (hello is null)
        {
            await RejectAsync(channel, socket, "agent.hello_expected", "Se esperaba un saludo antes de cualquier otro mensaje.", cancellationToken);
            return;
        }

        var handshake = await CompleteHandshakeAsync(credentials, hello, cancellationToken);

        if (handshake is not { IsSuccess: true, Value: AgentSession session })
        {
            await RejectAsync(channel, socket, handshake.Error!.Code, handshake.Error.Message, cancellationToken);
            return;
        }

        var connection = new WebSocketAgentConnection(session.TenantId, session.ServerId, channel, socket);

        if (!registry.TryRegister(connection))
        {
            await RejectAsync(channel, socket, "agent.already_connected", "El servidor ya tiene una conexión activa en esta instancia.", cancellationToken);
            return;
        }

        try
        {
            var welcome = new WelcomeMessage(session.ServerId, session.TenantId, session.HeartbeatSeconds, session.MaxConcurrentExecutions) { AgentToken = session.IssuedAgentToken };
            await channel.SendAsync<ServerToAgentMessage>(welcome, cancellationToken);

            // Enlazar la clave de enrutado y anunciar la presencia es lo que hace que un comando emitido
            // en otra instancia acabe llegando a este socket.
            await subscription.SubscribeAsync(session.ServerId, cancellationToken);
            await AnnouncePresenceAsync(session, connected: true);

            logger.LogInformation("Agente del servidor {ServerId} conectado a la instancia {InstanceId}", session.ServerId, options.Value.InstanceId);

            await PumpAsync(channel, session, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Conexión del servidor {ServerId} cerrada por parada del orquestador", session.ServerId);
        }
        catch (BaionProtocolException exception)
        {
            logger.LogWarning("El agente del servidor {ServerId} violó el protocolo: {Motivo}", session.ServerId, exception.Message);
        }
        catch (WebSocketException exception)
        {
            logger.LogInformation("El socket del servidor {ServerId} se cortó: {Motivo}", session.ServerId, exception.Message);
        }
        finally
        {
            registry.Remove(connection);
            await UnsubscribeAsync(session);
            await MarkDisconnectedAsync(session);
            await AnnouncePresenceAsync(session, connected: false);
        }
    }

    /// <summary>
    /// Un fallo al registrar el servidor tiene que volver como rechazo con motivo, no como un socket que
    /// muere de golpe: el agente reintentaría en bucle sin saber nunca qué le pasa.
    /// </summary>
    private async Task<Result<AgentSession>> CompleteHandshakeAsync(AgentCredentialContext credentials, HelloMessage hello, CancellationToken cancellationToken)
    {
        try
        {
            return await InScopeAsync(service => service.CompleteHandshakeAsync(credentials, hello, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falló el registro del servidor {MachineId} del tenant {TenantId}", hello.MachineId, credentials.TenantId);
            return Result<AgentSession>.Failure(Error.Unexpected("agent.handshake_failed", "El orquestador no pudo registrar el servidor."));
        }
    }

    /// <summary>Lee el saludo con un plazo propio, para que un cliente mudo no ocupe la conexión indefinidamente.</summary>
    private async Task<HelloMessage?> ReceiveHelloAsync(BaionMessageChannel channel, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.HandshakeTimeoutSeconds));

        try
        {
            return await channel.ReceiveAsync<AgentToServerMessage>(timeout.Token) as HelloMessage;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Se agotó el plazo de saludo del agente");
            return null;
        }
        catch (BaionProtocolException exception)
        {
            logger.LogWarning("Saludo inválido de un agente: {Motivo}", exception.Message);
            return null;
        }
    }

    private async Task PumpAsync(BaionMessageChannel channel, AgentSession session, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await channel.ReceiveAsync<AgentToServerMessage>(cancellationToken);

            if (message is null)
            {
                logger.LogInformation("El agente del servidor {ServerId} cerró la conexión", session.ServerId);
                return;
            }

            await DispatchAsync(session, message, cancellationToken);
        }
    }

    private async Task DispatchAsync(AgentSession session, AgentToServerMessage message, CancellationToken cancellationToken)
    {
        switch (message)
        {
            case HeartbeatMessage:
                await InScopeAsync(async service =>
                {
                    await service.TouchAsync(session.TenantId, session.ServerId, cancellationToken);
                    return true;
                });
                break;

            // Encolar y seguir leyendo: escribir en la base desde aquí frenaría el socket.
            case MetricsReportMessage report:
                metricQueue.TryEnqueue(new MetricSample(session.TenantId, session.ServerId, report));
                break;

            // Igual que las métricas, las novedades de ejecución se encolan y se escriben fuera del socket.
            case ScriptStartedMessage started:
                scriptQueue.TryEnqueue(new ScriptStartEvent(session.TenantId, started.ExecutionId, started.StartedAt));
                break;

            case ScriptOutputChunkMessage chunk:
                scriptQueue.TryEnqueue(new ScriptOutputEvent(session.TenantId, chunk.ExecutionId, chunk.Stream, chunk.Content));
                break;

            case ScriptCompletedMessage completed:
                scriptQueue.TryEnqueue(new ScriptCompletionEvent(session.TenantId, completed.ExecutionId, completed.Status, completed.ExitCode, completed.CompletedAt, completed.ErrorMessage));
                break;

            default:
                logger.LogDebug("Mensaje {MessageType} del servidor {ServerId} sin manejador todavía", message.GetType().Name, session.ServerId);
                break;
        }
    }

    private async Task AnnouncePresenceAsync(AgentSession session, bool connected)
    {
        try
        {
            await presence.PublishAsync(new AgentPresenceChanged(session.TenantId, session.ServerId, options.Value.InstanceId, connected, timeProvider.GetUtcNow()), CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning("No se pudo anunciar la presencia del servidor {ServerId}: {Motivo}", session.ServerId, exception.Message);
        }
    }

    private async Task UnsubscribeAsync(AgentSession session)
    {
        try
        {
            await subscription.UnsubscribeAsync(session.ServerId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning("No se pudo soltar la clave de enrutado del servidor {ServerId}: {Motivo}", session.ServerId, exception.Message);
        }
    }

    private async Task MarkDisconnectedAsync(AgentSession session)
    {
        try
        {
            // Sin el token de la conexión: el cierre debe registrarse aunque el orquestador esté parando.
            await InScopeAsync(async service =>
            {
                await service.MarkDisconnectedAsync(session.TenantId, session.ServerId, CancellationToken.None);
                return true;
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo marcar como desconectado el servidor {ServerId}", session.ServerId);
        }
    }

    private async Task RejectAsync(BaionMessageChannel channel, WebSocket socket, string code, string reason, CancellationToken cancellationToken)
    {
        logger.LogWarning("Handshake de agente rechazado: {Code} — {Reason}", code, reason);

        try
        {
            await channel.SendAsync<ServerToAgentMessage>(new ConnectionRejectedMessage(code, reason), cancellationToken);
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, code, cancellationToken);
        }
        catch (WebSocketException)
        {
            // El agente ya se fue; no hay nada que informar.
        }
    }

    private async Task<TResult> InScopeAsync<TResult>(Func<IAgentEnrollmentService, Task<TResult>> work)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await work(scope.ServiceProvider.GetRequiredService<IAgentEnrollmentService>());
    }
}
