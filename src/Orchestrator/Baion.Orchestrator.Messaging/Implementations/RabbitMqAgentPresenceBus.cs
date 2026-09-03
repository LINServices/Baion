using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Orchestrator.Models.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Baion.Orchestrator.Messaging.Implementations;

internal class RabbitMqAgentPresenceBus(RabbitMqConnectionProvider connections, IOptions<RabbitMqOptions> options, ILogger<RabbitMqAgentPresenceBus> logger) : IAgentPresenceBus, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IChannel? _channel;

    public async Task PublishAsync(AgentPresenceChanged notification, CancellationToken cancellationToken)
    {
        try
        {
            var channel = await GetChannelAsync(cancellationToken);
            var payload = JsonSerializer.SerializeToUtf8Bytes(notification, BaionProtocol.JsonOptions);
            var properties = new BasicProperties { ContentType = "application/json", DeliveryMode = DeliveryModes.Transient };

            await channel.BasicPublishAsync(options.Value.PresenceExchange, routingKey: string.Empty, mandatory: false, properties, payload, cancellationToken);
        }
        catch (Exception exception)
        {
            // La presencia es un aviso de cortesía: si no sale, la instancia obsoleta se entera igual
            // cuando su socket muera o cuando falle la entrega.
            logger.LogWarning("No se pudo anunciar la presencia del servidor {ServerId}: {Motivo}", notification.ServerId, exception.Message);
        }
    }

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
