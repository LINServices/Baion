using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Baion.Orchestrator.Services.Implementations;

internal class AgentRegistry : IAgentRegistry
{
    private readonly ConcurrentDictionary<Guid, IAgentConnection> _connections = new();

    public int Count => _connections.Count;

    public bool TryRegister(IAgentConnection connection) => _connections.TryAdd(connection.ServerId, connection);

    public bool TryGet(Guid serverId, [NotNullWhen(true)] out IAgentConnection? connection) => _connections.TryGetValue(serverId, out connection);

    // Comparando la instancia se evita que el cierre de una conexión vieja desaloje a la nueva
    // cuando un agente reconecta antes de que el socket anterior termine de morir.
    public void Remove(IAgentConnection connection) => _connections.TryRemove(new KeyValuePair<Guid, IAgentConnection>(connection.ServerId, connection));
}
