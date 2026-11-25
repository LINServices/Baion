using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Contracts;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Contracts.Messages;
using Xunit;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Support;

// Agente de mentira sobre un WebSocket real contra el orquestador de verdad. Deja que la prueba decida
// qué responde y cuándo, que es justo lo que hace falta para comprobar el despacho y el orden de la salida.
internal sealed class FakeAgent : IAsyncDisposable
{
    private const int ProcessId = 4242;

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

    // Contesta a las peticiones correlacionadas que llegan por el socket (listado, detalle, logs, control
    // de servicios). Devolver null deja el mensaje sin respuesta, para poder probar el timeout.
    public Func<ServerToAgentMessage, AgentToServerMessage?>? QueryResponder { get; set; }

    // Reutilizar el mismo machineId hace que el orquestador reconozca la máquina y no cree otro servidor.
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

    // Espera la siguiente orden de ejecución que llegue del orquestador.
    public async Task<ExecuteScriptMessage> NextOrderAsync()
    {
        using var espera = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        return await _orders.Reader.ReadAsync(espera.Token);
    }

    public async Task ReportStartedAsync(Guid executionId)
    {
        await SendAsync(new ScriptStartedMessage(executionId, DateTimeOffset.UtcNow, ProcessId));
    }

    public async Task ReportOutputAsync(Guid executionId, OutputStream stream, string content, long sequence = 1)
    {
        await SendAsync(new ScriptOutputChunkMessage(executionId, stream, sequence, content));
    }

    public async Task ReportCompletedAsync(Guid executionId, ExecutionStatus status, int? exitCode, string? errorMessage = null)
    {
        await SendAsync(new ScriptCompletedMessage(executionId, status, exitCode, DateTimeOffset.UtcNow, errorMessage));
    }

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

    private async Task SendAsync(AgentToServerMessage message)
    {
        await _channel.SendAsync(message, Timeout());
    }

    private async Task PumpAsync()
    {
        while (!_shutdown.IsCancellationRequested && await _channel.ReceiveAsync<ServerToAgentMessage>(_shutdown.Token) is { } message)
            await HandleAsync(message);
    }

    private async Task HandleAsync(ServerToAgentMessage message)
    {
        if (message is ExecuteScriptMessage order)
        {
            await _orders.Writer.WriteAsync(order, _shutdown.Token);
            return;
        }

        if (QueryResponder?.Invoke(message) is { } reply)
            await _channel.SendAsync(reply, _shutdown.Token);
    }

    private static CancellationToken Timeout()
    {
        return new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token;
    }
}
