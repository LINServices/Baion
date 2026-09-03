using System;
using System.IO;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Baion.Orchestrator.Messaging.Implementations;

/// <summary>
/// Cierre defensivo de canales y conexiones. Al parar el proceso el cliente puede haber desechado ya lo
/// que hay debajo, y una excepción al soltar un recurso que de todas formas se estaba tirando solo
/// enturbiaría la parada.
/// </summary>
internal static class RabbitMqDisposal
{
    public static async ValueTask CloseQuietlyAsync(this IChannel? channel)
    {
        if (channel is null)
        {
            return;
        }

        try
        {
            if (channel.IsOpen)
            {
                await channel.CloseAsync();
            }
        }
        catch (Exception exception) when (IsExpectedOnShutdown(exception))
        {
            // Ya estaba cerrado.
        }

        await DisposeQuietlyAsync(channel);
    }

    public static async ValueTask CloseQuietlyAsync(this IConnection? connection)
    {
        if (connection is null)
        {
            return;
        }

        try
        {
            if (connection.IsOpen)
            {
                await connection.CloseAsync();
            }
        }
        catch (Exception exception) when (IsExpectedOnShutdown(exception))
        {
            // Ya estaba cerrada.
        }

        await DisposeQuietlyAsync(connection);
    }

    private static async ValueTask DisposeQuietlyAsync(IAsyncDisposable disposable)
    {
        try
        {
            await disposable.DisposeAsync();
        }
        catch (Exception exception) when (IsExpectedOnShutdown(exception))
        {
            // Nada que liberar.
        }
    }

    private static bool IsExpectedOnShutdown(Exception exception) => exception is ObjectDisposedException or AlreadyClosedException or OperationInterruptedException or IOException;
}
