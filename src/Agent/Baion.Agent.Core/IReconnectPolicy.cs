using System;

namespace Baion.Agent.Core;

/// <summary>Decide cuánto esperar entre intentos de reconexión.</summary>
public interface IReconnectPolicy
{
    /// <summary>Espera antes del intento indicado, siendo 1 el primer reintento.</summary>
    TimeSpan GetDelay(int attempt);
}
