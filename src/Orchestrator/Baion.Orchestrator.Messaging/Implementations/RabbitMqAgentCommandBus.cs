using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Contracts.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Baion.Orchestrator.Messaging.Implementations;

/// <summary>
/// Entrega comandos a los agentes esté donde esté su socket. La entrega local se intenta primero: si el
/// agente está en este proceso no tiene sentido dar un rodeo por el broker.
/// </summary>
internal class RabbitMqAgentCommandBus(RabbitMqConnectionProvider connections, ILocalAgentDelivery local, IServiceScopeFactory scopeFactory, IOptions<RabbitMqOptions> options, ILogger<RabbitMqAgentCommandBus> logger) : IAgentCommandBus, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IChannel? _channel;

    public async Task<bool> TrySendAsync(Guid serverId, ServerToAgentMessage message, CancellationToken cancellationToken)
    {
        if (await local.TryDeliverAsync(serverId, message, cancellationToken))
        {
            return true;
        }

        // Publicar a ciegas dejaría el mensaje sin destino y a quien lo pidió sin saberlo, así que
        // primero se mira el registro de presencia. Vive en la base, así que necesita su propio scope:
        // el bus es singleton y el contexto de datos no lo es.
        var instancia = await ResolveInstanceAsync(serverId, cancellationToken);

        if (instancia is null)
        {
            return false;
        }

        var channel = await GetChannelAsync(cancellationToken);
        var payload = JsonSerializer.SerializeToUtf8Bytes(message, BaionProtocol.JsonOptions);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Transient,
            MessageId = message.MessageId.ToString()
        };

        await channel.BasicPublishAsync(options.Value.CommandExchange, BuildRoutingKey(serverId), mandatory: false, properties, payload, cancellationToken);
        logger.LogDebug("Comando {MessageType} publicado hacia el servidor {ServerId}, conectado en {InstanceId}", message.GetType().Name, serverId, instancia);

        return true;
    }

    private async Task<string?> ResolveInstanceAsync(Guid serverId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IAgentPresenceLookup>().ResolveInstanceAsync(serverId, cancellationToken);
    }

    /// <summary>Clave de enrutado por servidor: solo la instancia que tiene su socket está enlazada a ella.</summary>
    public static string BuildRoutingKey(Guid serverId) => $"agent.{serverId:N}";

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true } abierto)
        {
            return abierto;
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (_channel is { IsOpen: true } existente)
            {
                return existente;
            }

            var connection = await connections.GetAsync(cancellationToken);
            _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await connections.EnsureTopologyAsync(_channel, cancellationToken);

            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseQuietlyAsync();
        _gate.Dispose();
    }
}
