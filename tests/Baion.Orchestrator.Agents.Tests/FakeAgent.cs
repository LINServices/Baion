using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Contracts.Enums;
using Baion.Contracts.Messages;
using Xunit;

/// <summary>
/// Agente de mentira sobre un WebSocket real contra el orquestador de verdad. Deja que la prueba decida
/// qué responde y cuándo, que es justo lo que hace falta para comprobar el despacho y el orden de la salida.
/// </summary>
internal sealed class FakeAgent : IAsyncDisposable
{
    private readonly WebSocket _socket;

    private readonly BaionMessageChannel _channel;

    private readonly Channel<ExecuteScriptMessage> _orders = Channel.CreateUnbounded<ExecuteScriptMessage>();

    private readonly CancellationTokenSource _shutdown = new();

    private readonly Task _pump;

    private FakeAgent(WebSocket socket, BaionMessageChannel channel, Guid serverId, string machineId)
    {
        _socket = socket;
        _channel = channel;
        ServerId = serverId;
        MachineId = machineId;
        _pump = Task.Run(PumpAsync);
    }

    public Guid ServerId { get; }

    public string MachineId { get; }

    /// <summary>Reutilizar el mismo <paramref name="machineId"/> hace que el orquestador reconozca la máquina y no cree otro servidor.</summary>
    public static async Task<FakeAgent> ConnectAsync(OrchestratorFactory factory, string enrollmentToken, ServerPlatform platform = ServerPlatform.Linux, string? machineId = null)
    {
        var client = factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers[BaionProtocol.EnrollmentTokenHeader] = enrollmentToken;

        var socket = await client.ConnectAsync(new Uri(factory.Server.BaseAddress, BaionProtocol.WebSocketPath.TrimStart('/')), Timeout());
        var channel = new BaionMessageChannel(socket);

        machineId ??= Guid.NewGuid().ToString("N");
        var runtimeIdentifier = platform is ServerPlatform.Windows ? "win-x64" : "linux-x64";
        var hello = new HelloMessage(BaionProtocol.Version, platform, runtimeIdentifier, "1.0.0", $"host-{machineId[..8]}", machineId, 4, 8_000_000_000);

        await channel.SendAsync<AgentToServerMessage>(hello, Timeout());
        var welcome = Assert.IsType<WelcomeMessage>(await channel.ReceiveAsync<ServerToAgentMessage>(Timeout()));

        return new FakeAgent(socket, channel, welcome.ServerId, machineId);
    }

    /// <summary>Espera la siguiente orden de ejecución que llegue del orquestador.</summary>
    public async Task<ExecuteScriptMessage> NextOrderAsync()
    {
        using var espera = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        return await _orders.Reader.ReadAsync(espera.Token);
    }

    public async Task ReportStartedAsync(Guid executionId) => await SendAsync(new ScriptStartedMessage(executionId, DateTimeOffset.UtcNow, ProcessId));

    public async Task ReportOutputAsync(Guid executionId, OutputStream stream, string content, long sequence = 1) => await SendAsync(new ScriptOutputChunkMessage(executionId, stream, sequence, content));

    public async Task ReportCompletedAsync(Guid executionId, ExecutionStatus status, int? exitCode, string? errorMessage = null) => await SendAsync(new ScriptCompletedMessage(executionId, status, exitCode, DateTimeOffset.UtcNow, errorMessage));

    public async ValueTask DisposeAsync()
    {
        await _shutdown.CancelAsync();

        try
        {
            await _pump;
        }
        catch (Exception)
        {
            // El bucle muere con el socket; no hay nada que rescatar al cerrar.
        }

        _channel.Dispose();
        _socket.Dispose();
        _shutdown.Dispose();
    }

    private async Task SendAsync(AgentToServerMessage message) => await _channel.SendAsync(message, Timeout());

    private async Task PumpAsync()
    {
        while (!_shutdown.IsCancellationRequested)
        {
            var message = await _channel.ReceiveAsync<ServerToAgentMessage>(_shutdown.Token);

            if (message is null)
            {
                return;
            }

            if (message is ExecuteScriptMessage order)
            {
                await _orders.Writer.WriteAsync(order, _shutdown.Token);
            }
        }
    }

    private static CancellationToken Timeout() => new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token;

    private const int ProcessId = 4242;
}
