using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Contracts.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Baion.Orchestrator.Messaging.Implementations;

/// <summary>
/// Cola exclusiva de esta instancia sobre el exchange de comandos. Se enlaza una clave por cada agente cuyo
/// socket vive aquí, así que el enrutado lo resuelve RabbitMQ: un comando solo llega al proceso que puede
/// entregarlo, sin que nadie tenga que consultar dónde está cada agente.
/// </summary>
internal class RabbitMqAgentCommandSubscription(RabbitMqConnectionProvider connections, ILocalAgentDelivery local, IOptions<RabbitMqOptions> options, ILogger<RabbitMqAgentCommandSubscription> logger) : IAgentCommandSubscription, IHostedService, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IChannel? _channel;

    private string? _queue;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await EnsureConsumerAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            // El orquestador arranca igual: sin broker sigue atendiendo a sus propios agentes, y la
            // suscripción se reintenta la primera vez que alguien se conecte.
            logger.LogError(exception, "No se pudo abrir la cola de comandos; esta instancia solo alcanzará a sus agentes locales");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task SubscribeAsync(Guid serverId, CancellationToken cancellationToken)
    {
        var channel = await EnsureConsumerAsync(cancellationToken);
        await channel.QueueBindAsync(_queue!, options.Value.CommandExchange, RabbitMqAgentCommandBus.BuildRoutingKey(serverId), cancellationToken: cancellationToken);

        logger.LogDebug("Esta instancia atiende ya los comandos del servidor {ServerId}", serverId);
    }

    public async Task UnsubscribeAsync(Guid serverId, CancellationToken cancellationToken)
    {
        if (_channel is not { IsOpen: true } channel || _queue is null)
        {
            return;
        }

        await channel.QueueUnbindAsync(_queue, options.Value.CommandExchange, RabbitMqAgentCommandBus.BuildRoutingKey(serverId), cancellationToken: cancellationToken);
        logger.LogDebug("Esta instancia deja de atender los comandos del servidor {ServerId}", serverId);
    }

    private async Task<IChannel> EnsureConsumerAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true } abierto && _queue is not null)
        {
            return abierto;
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (_channel is { IsOpen: true } existente && _queue is not null)
            {
                return existente;
            }

            var connection = await connections.GetAsync(cancellationToken);
            _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await connections.EnsureTopologyAsync(_channel, cancellationToken);

            // Exclusiva y efímera: si la instancia cae, la cola y sus enlaces desaparecen con ella y
            // ningún comando queda encolado para un proceso que ya no existe.
            var declared = await _channel.QueueDeclareAsync(queue: string.Empty, durable: false, exclusive: true, autoDelete: true, cancellationToken: cancellationToken);
            _queue = declared.QueueName;

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnReceivedAsync;

            await _channel.BasicConsumeAsync(_queue, autoAck: true, consumer, cancellationToken);
            logger.LogInformation("Cola de comandos {Cola} abierta para esta instancia", _queue);

            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs delivery)
    {
        try
        {
            var message = JsonSerializer.Deserialize<ServerToAgentMessage>(delivery.Body.Span, BaionProtocol.JsonOptions);

            if (message is null)
            {
                logger.LogWarning("Llegó un comando vacío por la clave {RoutingKey}", delivery.RoutingKey);
                return;
            }

            if (!TryParseServerId(delivery.RoutingKey, out var serverId))
            {
                logger.LogWarning("Clave de enrutado no reconocida: {RoutingKey}", delivery.RoutingKey);
                return;
            }

            // Puede que el agente se haya ido entre la publicación y la entrega; el emisor se entera por
            // el plazo de la ejecución, no por una confirmación.
            if (!await local.TryDeliverAsync(serverId, message, CancellationToken.None))
            {
                logger.LogWarning("Llegó un comando para el servidor {ServerId}, que ya no está conectado aquí", serverId);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo procesar un comando recibido por {RoutingKey}", delivery.RoutingKey);
        }
    }

    private static bool TryParseServerId(string routingKey, out Guid serverId)
    {
        serverId = Guid.Empty;
        var separador = routingKey.LastIndexOf('.');

        return separador >= 0 && Guid.TryParseExact(routingKey[(separador + 1)..], "N", out serverId);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseQuietlyAsync();
        _gate.Dispose();
    }
}
