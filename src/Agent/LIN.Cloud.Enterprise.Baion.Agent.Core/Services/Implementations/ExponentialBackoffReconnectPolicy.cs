using System;
using Microsoft.Extensions.Options;

namespace LIN.Cloud.Enterprise.Baion.Agent.Core.Implementations;

// Jitter completo dentro de la ventana: el azar evita que, al reiniciarse el orquestador,
// todos los agentes vuelvan a la vez y lo tumben de nuevo.
internal class ExponentialBackoffReconnectPolicy(IOptions<AgentOptions> options) : IReconnectPolicy
{
    private const int MaxExponent = 16;

    public TimeSpan GetDelay(int attempt)
    {
        var settings = options.Value;
        var minimum = Math.Max(1, settings.MinReconnectSeconds);
        var maximum = Math.Max(minimum, settings.MaxReconnectSeconds);

        // El exponente se acota antes de desplazar para que no desborde en conexiones que llevan días fallando.
        var exponent = Math.Min(Math.Max(attempt, 1) - 1, MaxExponent);
        var ceiling = Math.Min((double)maximum, minimum * Math.Pow(2, exponent));

        return TimeSpan.FromSeconds(Random.Shared.NextDouble() * (ceiling - minimum) + minimum);
    }
}
