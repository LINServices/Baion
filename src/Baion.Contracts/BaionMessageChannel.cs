using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Baion.Contracts;

/// <summary>
/// Encuadra mensajes JSON sobre un WebSocket. La usan los dos extremos para que el formato de trama
/// no pueda divergir entre orquestador y agente.
/// </summary>
/// <remarks>
/// <see cref="SendAsync"/> es seguro desde varios hilos; <see cref="ReceiveAsync"/> no: el canal asume
/// un único lector, que es como funciona el bucle de recepción de ambos extremos.
/// </remarks>
public sealed class BaionMessageChannel(WebSocket socket, int maxMessageBytes = BaionMessageChannel.DefaultMaxMessageBytes) : IDisposable
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private readonly byte[] _receiveBuffer = ArrayPool<byte>.Shared.Rent(ReceiveChunkBytes);

    private bool _disposed;

    /// <summary>Serializa y envía un mensaje como una única trama de texto.</summary>
    public async Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(message, BaionProtocol.JsonOptions);

        await _sendLock.WaitAsync(cancellationToken);

        try
        {
            await socket.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>Lee el siguiente mensaje, reensamblando las tramas parciales. Devuelve null si el otro extremo cerró.</summary>
    public async Task<TMessage?> ReceiveAsync<TMessage>(CancellationToken cancellationToken) where TMessage : class
    {
        var accumulated = new ArrayBufferWriter<byte>(ReceiveChunkBytes);

        while (true)
        {
            var received = await socket.ReceiveAsync(_receiveBuffer.AsMemory(0, ReceiveChunkBytes), cancellationToken);

            if (received.MessageType is WebSocketMessageType.Close)
            {
                return null;
            }

            accumulated.Write(_receiveBuffer.AsSpan(0, received.Count));

            if (accumulated.WrittenCount > maxMessageBytes)
            {
                throw new BaionProtocolException($"El mensaje supera el máximo de {maxMessageBytes} bytes.");
            }

            if (received.EndOfMessage)
            {
                break;
            }
        }

        try
        {
            return JsonSerializer.Deserialize<TMessage>(accumulated.WrittenSpan, BaionProtocol.JsonOptions) ?? throw new BaionProtocolException("El mensaje recibido es nulo.");
        }
        catch (JsonException exception)
        {
            throw new BaionProtocolException($"No se pudo interpretar el mensaje recibido: {exception.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ArrayPool<byte>.Shared.Return(_receiveBuffer);
        _sendLock.Dispose();
    }

    /// <summary>Tope por mensaje. El output de scripts viaja troceado, así que ninguna trama debería acercarse.</summary>
    public const int DefaultMaxMessageBytes = 1024 * 1024;

    private const int ReceiveChunkBytes = 8 * 1024;
}
