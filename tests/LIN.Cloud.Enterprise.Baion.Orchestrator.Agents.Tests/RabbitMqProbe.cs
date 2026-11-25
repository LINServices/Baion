using System;
using System.Net.Sockets;

/// <summary>
/// Comprueba si hay un broker a mano. Lo consultan el atributo que omite las pruebas y el fixture que
/// levanta las dos instancias, para que sin broker no se monte una infraestructura que nadie va a usar.
/// </summary>
internal static class RabbitMqProbe
{
    /// <summary>
    /// La comprobación es deliberadamente síncrona, sin <c>Task</c> por medio: el atributo la ejecuta
    /// durante el descubrimiento de xUnit, y bloquear ahí sobre una tarea cuelga el host de pruebas
    /// justo cuando el broker sí responde.
    /// </summary>
    public static bool IsReachable()
    {
        try
        {
            using var client = new TcpClient();
            var conexion = client.BeginConnect(Host, Port, null, null);

            if (!conexion.AsyncWaitHandle.WaitOne(ProbeTimeout))
            {
                return false;
            }

            client.EndConnect(conexion);
            return true;
        }
        catch (Exception exception) when (exception is SocketException or ObjectDisposedException or InvalidOperationException)
        {
            return false;
        }
    }

    public static string Description => $"{Host}:{Port}";

    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

    private const string Host = "localhost";

    private const int Port = 5672;
}
