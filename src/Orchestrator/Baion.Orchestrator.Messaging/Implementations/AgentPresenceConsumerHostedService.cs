using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Orchestrator.Models.Dtos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Baion.Orchestrator.Messaging.Implementations;

/// <summary>
/// Escucha los cambios de presencia de todas las instancias. Sirve para un caso concreto: si un agente
/// reconecta en otra instancia antes de que aquí muera su socket viejo, esta instancia se quedaría con una
/// conexión zombi que podría tragarse comandos. Al enterarse, la cierra.
/// </summary>
internal class AgentPresenceConsumerHostedService(RabbitMqConnectionProvider connections, ILocalAgentDelivery local, IOptions<RabbitMqOptions> options, ILogger<AgentPresenceConsumerHostedService> logger) : IHostedService, IAsyncDisposable
{
    private IChannel? _channel;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var connection = await connections.GetAsync(cancellationToken);
            _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await connections.EnsureTopologyAsync(_channel, cancellationToken);

            var declared = await _channel.QueueDeclareAsync(queue: string.Empty, durable: false, exclusive: true, autoDelete: true, cancellationToken: cancellationToken);
            await _channel.QueueBindAsync(declared.QueueName, options.Value.PresenceExchange, routingKey: string.Empty, cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnReceivedAsync;

            await _channel.BasicConsumeAsync(declared.QueueName, autoAck: true, consumer, cancellationToken);
            logger.LogInformation("Escuchando los cambios de presencia en {Cola}", declared.QueueName);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo escuchar los cambios de presencia");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs delivery)
    {
        try
        {
            var notification = JsonSerializer.Deserialize<AgentPresenceChanged>(delivery.Body.Span, BaionProtocol.JsonOptions);

            if (notification is not { Connected: true })
            {
                return;
            }

            // El propio anuncio vuelve por el fanout: quien recibe compara con su identidad y lo ignora.
            await local.EvictAsync(notification.ServerId, notification.InstanceId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo procesar un cambio de presencia");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseQuietlyAsync();
    }
}
