using System;
using System.Threading.Tasks;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Support;

// Sondeo compartido por las pruebas: el orquestador procesa los mensajes del agente de forma
// asíncrona, así que lo observable en la base aparece al cabo de un instante.
internal static class Polling
{
    private const int IntervalMilliseconds = 100;

    // Repite la lectura hasta que el valor cumpla la condición o se agoten los intentos.
    // Devuelve siempre la última lectura, para que la prueba pueda aserciar sobre ella.
    public static async Task<T> WaitForAsync<T>(Func<Task<T>> read, Func<T, bool> isReady, int attempts)
    {
        var value = await read();
        var attempt = 1;

        while (!isReady(value) && attempt++ < attempts)
        {
            await Task.Delay(IntervalMilliseconds);
            value = await read();
        }

        return value;
    }

    public static Task<bool> WaitForAsync(Func<Task<bool>> condition, int attempts)
    {
        return WaitForAsync(condition, satisfied => satisfied, attempts);
    }
}
