using System;
using System.Diagnostics.CodeAnalysis;

namespace Baion.Orchestrator.Services;

/// <summary>
/// Agentes conectados a <b>esta</b> instancia. En la fase de multi-instancia, RabbitMQ enruta los comandos
/// hasta la instancia que figura en el registro de presencia; este registro es el tramo local de ese camino.
/// </summary>
public interface IAgentRegistry
{
    /// <summary>Registra la conexión de un servidor. Devuelve false si ya había otra activa aquí.</summary>
    bool TryRegister(IAgentConnection connection);

    /// <summary>Obtiene la conexión abierta con un servidor, si la tiene esta instancia.</summary>
    bool TryGet(Guid serverId, [NotNullWhen(true)] out IAgentConnection? connection);

    /// <summary>Quita la conexión, solo si sigue siendo la que se registró.</summary>
    void Remove(IAgentConnection connection);

    /// <summary>Número de agentes conectados a esta instancia.</summary>
    int Count { get; }
}
