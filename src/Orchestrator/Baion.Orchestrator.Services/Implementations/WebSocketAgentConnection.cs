using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Contracts.Messages;

namespace Baion.Orchestrator.Services.Implementations;

internal sealed class WebSocketAgentConnection(Guid tenantId, Guid serverId, BaionMessageChannel channel, WebSocket socket) : IAgentConnection
{
    public Guid TenantId => tenantId;

    public Guid ServerId => serverId;

    public async Task SendAsync(ServerToAgentMessage message, CancellationToken cancellationToken) => await channel.SendAsync(message, cancellationToken);

    public async Task CloseAsync(string reason)
    {
        try
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
        }
        catch (Exception exception) when (exception is WebSocketException or ObjectDisposedException or InvalidOperationException)
        {
            // El socket ya estaba muerto: es exactamente el estado al que se quería llegar.
        }
    }
}
