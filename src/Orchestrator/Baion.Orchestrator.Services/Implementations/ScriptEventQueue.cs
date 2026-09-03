using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Orchestrator.Services.Implementations;

internal class ScriptEventQueue : IScriptEventQueue
{
    private readonly Channel<ScriptExecutionEvent> _channel;

    private readonly ILogger<ScriptEventQueue> _logger;

    private int _pending;

    public ScriptEventQueue(IOptions<ScriptEventOptions> options, ILogger<ScriptEventQueue> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<ScriptExecutionEvent>(new BoundedChannelOptions(options.Value.QueueCapacity)
        {
            // Descartar deja la salida incompleta, así que el buzón es holgado y el llenado se registra
            // como error; aun así se prefiere eso a frenar el hilo del socket.
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public int PendingCount => Volatile.Read(ref _pending);

    public bool TryEnqueue(ScriptExecutionEvent notification)
    {
        if (_channel.Writer.TryWrite(notification))
        {
            Interlocked.Increment(ref _pending);
            return true;
        }

        _logger.LogError("Buzón de novedades de ejecución lleno; se descartó {Tipo} de la ejecución {ExecutionId}", notification.GetType().Name, notification.ExecutionId);
        return false;
    }

    /// <summary>Toma una novedad si la hay, sin esperar.</summary>
    internal bool TryRead([MaybeNullWhen(false)] out ScriptExecutionEvent notification)
    {
        if (!_channel.Reader.TryRead(out notification))
        {
            return false;
        }

        Interlocked.Decrement(ref _pending);
        return true;
    }

    /// <summary>Espera a que haya algo que leer. Devuelve false cuando el buzón se cierra.</summary>
    internal ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken) => _channel.Reader.WaitToReadAsync(cancellationToken);
}
