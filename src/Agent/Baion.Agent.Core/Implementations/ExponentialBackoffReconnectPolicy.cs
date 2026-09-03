using System;
using Microsoft.Extensions.Options;

namespace Baion.Agent.Core.Implementations;

/// <summary>
/// Retroceso exponencial con jitter completo dentro de la ventana. El azar es lo que evita que, al
/// reiniciarse el orquestador, todos los agentes vuelvan a la vez y lo tumben de nuevo.
/// </summary>
internal class ExponentialBackoffReconnectPolicy(IOptions<AgentOptions> options) : IReconnectPolicy
{
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

    private const int MaxExponent = 16;
}
