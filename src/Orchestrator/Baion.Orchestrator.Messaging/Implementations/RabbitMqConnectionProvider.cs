using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Baion.Orchestrator.Messaging.Implementations;

/// <summary>
/// Mantiene una única conexión a RabbitMQ para todo el proceso. El cliente ya reconecta solo, así que aquí
/// solo hace falta abrirla la primera vez que alguien la pide y no abrir una por publicación.
/// </summary>
internal class RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnectionProvider> logger) : IHostedService, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IConnection? _connection;

    public async Task<IConnection> GetAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } abierta)
        {
            return abierta;
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (_connection is { IsOpen: true } existente)
            {
                return existente;
            }

            var settings = options.Value;

            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                VirtualHost = settings.VirtualHost,
                UserName = settings.UserName,
                Password = settings.Password,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                ClientProvidedName = $"baion-orchestrator-{Environment.MachineName}-{Environment.ProcessId}"
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            logger.LogInformation("Conectado a RabbitMQ en {Host}:{Puerto}{VHost}", settings.HostName, settings.Port, settings.VirtualHost);

            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Declara los exchanges. Es idempotente, así que puede llamarse en cada arranque.</summary>
    public async Task EnsureTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        await channel.ExchangeDeclareAsync(settings.CommandExchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(settings.PresenceExchange, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: cancellationToken);
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// La conexión se cierra aquí y no solo al liberar el contenedor. Con la recuperación automática
    /// activada, el cliente mantiene maquinaria de reconexión que impide que el proceso termine si nadie
    /// la para; la parada del host sí es un momento determinista para hacerlo.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken) => await CloseAsync();

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        _gate.Dispose();
    }

    private async Task CloseAsync()
    {
        var connection = Interlocked.Exchange(ref _connection, null);
        await connection.CloseQuietlyAsync();
    }
}
