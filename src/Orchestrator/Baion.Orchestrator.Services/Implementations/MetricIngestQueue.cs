using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Orchestrator.Services.Implementations;

internal class MetricIngestQueue : IMetricIngestQueue
{
    private readonly Channel<MetricSample> _channel;

    private readonly ILogger<MetricIngestQueue> _logger;

    private int _pending;

    private int _dropped;

    public MetricIngestQueue(IOptions<MetricIngestOptions> options, ILogger<MetricIngestQueue> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<MetricSample>(new BoundedChannelOptions(options.Value.QueueCapacity)
        {
            // Descartar es deliberado: la alternativa es que el escritor lento frene al hilo del socket.
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public int PendingCount => Volatile.Read(ref _pending);

    public bool TryEnqueue(MetricSample sample)
    {
        if (_channel.Writer.TryWrite(sample))
        {
            Interlocked.Increment(ref _pending);
            return true;
        }

        var descartadas = Interlocked.Increment(ref _dropped);

        // Se avisa de forma espaciada: si el buzón se llena, se llena muchas veces por segundo.
        if (descartadas % DropLogInterval == 1)
        {
            _logger.LogWarning("Buzón de métricas lleno; se descartaron {Descartadas} muestras en total", descartadas);
        }

        return false;
    }

    /// <summary>Toma una muestra si la hay, sin esperar.</summary>
    internal bool TryRead([MaybeNullWhen(false)] out MetricSample sample)
    {
        if (!_channel.Reader.TryRead(out sample))
        {
            return false;
        }

        Interlocked.Decrement(ref _pending);
        return true;
    }

    /// <summary>Espera a que haya algo que leer. Devuelve false cuando el buzón se cierra.</summary>
    internal ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken) => _channel.Reader.WaitToReadAsync(cancellationToken);

    private const int DropLogInterval = 1000;
}
