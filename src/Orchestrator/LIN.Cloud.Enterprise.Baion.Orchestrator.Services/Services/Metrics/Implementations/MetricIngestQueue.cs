using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services.Implementations;

internal class MetricIngestQueue(IOptions<MetricIngestOptions> options, ILogger<MetricIngestQueue> logger) : IMetricIngestQueue
{
    private const int DropLogInterval = 1000;

    private readonly Channel<MetricSample> _channel = Channel.CreateBounded<MetricSample>(new BoundedChannelOptions(options.Value.QueueCapacity)
    {
        // Descartar es deliberado: la alternativa es que el escritor lento frene al hilo del socket.
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
        SingleWriter = false
    });

    private int _pending;

    private int _dropped;

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
            logger.LogWarning("Buzón de métricas lleno; se descartaron {Descartadas} muestras en total", descartadas);

        return false;
    }

    internal bool TryRead([MaybeNullWhen(false)] out MetricSample sample)
    {
        if (!_channel.Reader.TryRead(out sample))
            return false;

        Interlocked.Decrement(ref _pending);
        return true;
    }

    // Espera a que haya algo que leer. Devuelve false cuando el buzón se cierra.
    internal ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.WaitToReadAsync(cancellationToken);
    }
}
