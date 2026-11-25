using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LIN.Cloud.Enterprise.Baion.Contracts;

// Encuadra mensajes JSON sobre un WebSocket en los dos extremos, para que el formato de trama no pueda divergir.
// SendAsync es seguro desde varios hilos; ReceiveAsync asume un único lector.
public sealed class BaionMessageChannel(WebSocket socket, int maxMessageBytes = BaionMessageChannel.DefaultMaxMessageBytes) : IDisposable
{
    // El output de scripts viaja troceado, así que ninguna trama debería acercarse a este tope
    public const int DefaultMaxMessageBytes = 1024 * 1024;

    private const int ReceiveChunkBytes = 8 * 1024;

    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private readonly byte[] _receiveBuffer = ArrayPool<byte>.Shared.Rent(ReceiveChunkBytes);

    private bool _disposed;

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

    // Devuelve null si el otro extremo cerró
    public async Task<TMessage?> ReceiveAsync<TMessage>(CancellationToken cancellationToken) where TMessage : class
    {
        var accumulated = new ArrayBufferWriter<byte>(ReceiveChunkBytes);

        ValueWebSocketReceiveResult received;

        do
        {
            received = await ReceiveChunkAsync(accumulated, cancellationToken);
        }
        while (received.MessageType is not WebSocketMessageType.Close && !received.EndOfMessage);

        if (received.MessageType is WebSocketMessageType.Close)
            return null;

        try
        {
            return JsonSerializer.Deserialize<TMessage>(accumulated.WrittenSpan, BaionProtocol.JsonOptions) ?? throw new BaionProtocolException("El mensaje recibido es nulo.");
        }
        catch (JsonException exception)
        {
            throw new BaionProtocolException($"No se pudo interpretar el mensaje recibido: {exception.Message}");
        }
    }

    // Lee un fragmento y lo acumula, validando el tope de tamaño del mensaje
    private async Task<ValueWebSocketReceiveResult> ReceiveChunkAsync(ArrayBufferWriter<byte> accumulated, CancellationToken cancellationToken)
    {
        var received = await socket.ReceiveAsync(_receiveBuffer.AsMemory(0, ReceiveChunkBytes), cancellationToken);

        accumulated.Write(_receiveBuffer.AsSpan(0, received.Count));

        if (accumulated.WrittenCount > maxMessageBytes)
            throw new BaionProtocolException($"El mensaje supera el máximo de {maxMessageBytes} bytes.");

        return received;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ArrayPool<byte>.Shared.Return(_receiveBuffer);
        _sendLock.Dispose();
    }
}
