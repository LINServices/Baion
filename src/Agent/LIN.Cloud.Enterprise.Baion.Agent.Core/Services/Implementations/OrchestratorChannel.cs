using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Contracts;
using LIN.Cloud.Enterprise.Baion.Contracts.Messages;
using Microsoft.Extensions.Logging;

namespace LIN.Cloud.Enterprise.Baion.Agent.Core.Implementations;

// Lo fija y lo suelta AgentConnectionWorker; el resto del agente solo ve la interfaz y no se entera de las reconexiones.
internal class OrchestratorChannel(ILogger<OrchestratorChannel> logger) : IOrchestratorChannel
{
    private BaionMessageChannel? _current;

    private AgentSessionInfo? _session;

    public bool IsConnected => Volatile.Read(ref _current) is not null;

    public AgentSessionInfo? Session => Volatile.Read(ref _session);

    public async Task<bool> TrySendAsync(AgentToServerMessage message, CancellationToken cancellationToken)
    {
        var channel = Volatile.Read(ref _current);

        if (channel is null)
            return false;

        try
        {
            await channel.SendAsync(message, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // El socket ya se está cayendo; el bucle de conexión se encarga de reconectar.
            logger.LogDebug("No se pudo enviar {MessageType}: {Motivo}", message.GetType().Name, exception.Message);
            return false;
        }
    }

    internal void Attach(BaionMessageChannel channel, AgentSessionInfo session)
    {
        Volatile.Write(ref _session, session);
        Volatile.Write(ref _current, channel);
    }

    internal void Detach(BaionMessageChannel channel)
    {
        if (Interlocked.CompareExchange(ref _current, null, channel) == channel)
            Volatile.Write(ref _session, null);
    }
}
