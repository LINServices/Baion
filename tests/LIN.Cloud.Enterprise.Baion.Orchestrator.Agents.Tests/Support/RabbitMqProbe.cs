using System;
using System.Net.Sockets;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Support;

// Comprueba si hay un broker a mano. Lo consultan el atributo que omite las pruebas y el fixture que
// levanta las dos instancias, para que sin broker no se monte una infraestructura que nadie va a usar.
internal static class RabbitMqProbe
{
    private const string Host = "localhost";

    private const int Port = 5672;

    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

    public static string Description => $"{Host}:{Port}";

    // La comprobación es deliberadamente síncrona, sin Task por medio: el atributo la ejecuta
    // durante el descubrimiento de xUnit, y bloquear ahí sobre una tarea cuelga el host de pruebas
    // justo cuando el broker sí responde.
    public static bool IsReachable()
    {
        try
        {
            using var client = new TcpClient();
            var conexion = client.BeginConnect(Host, Port, null, null);

            if (!conexion.AsyncWaitHandle.WaitOne(ProbeTimeout))
                return false;

            client.EndConnect(conexion);
            return true;
        }
        catch (Exception exception) when (exception is SocketException or ObjectDisposedException or InvalidOperationException)
        {
            return false;
        }
    }
}
