using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Baion.Agent.Core;
using Baion.Agent.Services;
using Baion.Agent.Services.Implementations;
using Baion.Contracts.Enums;
using Baion.Contracts.Messages;
using Baion.Contracts.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Baion.Agent.Services.Tests;

public class ServiceQueryProcessorTests
{
    private static readonly ServiceDetail Detalle = new(
        "nginx.service", "nginx", "web server", ServiceRuntimeState.Running, "active (running)", "running",
        ServiceStartupMode.Automatic, 1234, DateTimeOffset.UnixEpoch, 4096, "/lib/systemd/system/nginx.service", null, []);

    [Fact]
    public async Task Enqueue_ConPeticionDeListado_RespondeConServicesListedYElMismoRequestId()
    {
        var peticion = new ListServicesRequestMessage(Guid.NewGuid(), null);
        var inspector = new FakeInspector
        {
            OnList = _ => ServiceQueryOutcome<IReadOnlyList<ServiceSummary>>.Ok(
                [new ServiceSummary("nginx.service", "nginx", ServiceRuntimeState.Running, "active (running)", ServiceStartupMode.Automatic)])
        };

        var respuesta = await ProcesarAsync(inspector, peticion);

        var listado = Assert.IsType<ServicesListedMessage>(respuesta);
        Assert.Equal(peticion.RequestId, listado.RequestId);
        Assert.Single(listado.Services);
    }

    [Fact]
    public async Task Enqueue_ConPeticionDeDetalle_RespondeConServiceDescribed()
    {
        var peticion = new DescribeServiceRequestMessage(Guid.NewGuid(), "nginx.service");
        var inspector = new FakeInspector { OnDescribe = _ => ServiceQueryOutcome<ServiceDetail>.Ok(Detalle) };

        var respuesta = await ProcesarAsync(inspector, peticion);

        var descrito = Assert.IsType<ServiceDescribedMessage>(respuesta);
        Assert.Equal(peticion.RequestId, descrito.RequestId);
        Assert.Equal("nginx.service", descrito.Service.Id);
    }

    [Fact]
    public async Task Enqueue_ConPeticionDeLogs_RespondeConServiceLogsYPropagaElTruncado()
    {
        var peticion = new ServiceLogsRequestMessage(Guid.NewGuid(), "nginx.service", 100, null);
        var inspector = new FakeInspector
        {
            OnLogs = (_, _, _) => ServiceQueryOutcome<ServiceLogPage>.Ok(
                new ServiceLogPage([new ServiceLogLine(null, "info", "hola")], Truncated: true))
        };

        var respuesta = await ProcesarAsync(inspector, peticion);

        var logs = Assert.IsType<ServiceLogsMessage>(respuesta);
        Assert.Equal(peticion.RequestId, logs.RequestId);
        Assert.True(logs.Truncated);
        Assert.Single(logs.Lines);
    }

    [Fact]
    public async Task Enqueue_ConPeticionDeControl_RespondeConServiceControlled()
    {
        var peticion = new ControlServiceRequestMessage(Guid.NewGuid(), "nginx.service", ServiceControlAction.Restart);
        var inspector = new FakeInspector { OnControl = (_, _) => ServiceQueryOutcome<ServiceDetail>.Ok(Detalle) };

        var respuesta = await ProcesarAsync(inspector, peticion);

        var controlado = Assert.IsType<ServiceControlledMessage>(respuesta);
        Assert.Equal(peticion.RequestId, controlado.RequestId);
    }

    [Fact]
    public async Task Enqueue_CuandoElInspectorDevuelveFallo_RespondeConServiceQueryFailed()
    {
        var peticion = new DescribeServiceRequestMessage(Guid.NewGuid(), "fantasma.service");
        var inspector = new FakeInspector { OnDescribe = _ => ServiceQueryOutcome<ServiceDetail>.NotFound("no existe") };

        var respuesta = await ProcesarAsync(inspector, peticion);

        var fallo = Assert.IsType<ServiceQueryFailedMessage>(respuesta);
        Assert.Equal(peticion.RequestId, fallo.RequestId);
        Assert.Equal(ServiceQueryFailedMessage.NotFoundCode, fallo.Code);
        Assert.Equal("no existe", fallo.Message);
    }

    [Fact]
    public async Task Enqueue_CuandoElInspectorLanzaExcepcion_RespondeConToolFailed()
    {
        var peticion = new DescribeServiceRequestMessage(Guid.NewGuid(), "nginx.service");
        var inspector = new FakeInspector { OnDescribe = _ => throw new InvalidOperationException("systemctl reventó") };

        var respuesta = await ProcesarAsync(inspector, peticion);

        var fallo = Assert.IsType<ServiceQueryFailedMessage>(respuesta);
        Assert.Equal(ServiceQueryFailedMessage.ToolFailedCode, fallo.Code);
        Assert.Contains("systemctl reventó", fallo.Message);
    }

    [Fact]
    public async Task Enqueue_ConElMismoRequestIdDosVeces_SoloProcesaUna()
    {
        var peticion = new ListServicesRequestMessage(Guid.NewGuid(), null);
        var arranque = new TaskCompletionSource();
        var sueltalo = new TaskCompletionSource();
        var veces = 0;

        var inspector = new FakeInspector
        {
            OnList = _ =>
            {
                Interlocked.Increment(ref veces);
                arranque.TrySetResult();
                sueltalo.Task.GetAwaiter().GetResult();
                return ServiceQueryOutcome<IReadOnlyList<ServiceSummary>>.Ok([]);
            }
        };

        var canal = new FakeChannel();
        var procesador = CrearProcesador(inspector, canal);

        procesador.Enqueue(peticion);
        await arranque.Task.WaitAsync(TimeSpan.FromSeconds(5));
        procesador.Enqueue(peticion);
        sueltalo.SetResult();

        await canal.Esperar(TimeSpan.FromSeconds(5));
        await procesador.StopAsync(CancellationToken.None);

        Assert.Equal(1, veces);
    }

    private static async Task<AgentToServerMessage> ProcesarAsync(FakeInspector inspector, ServerToAgentMessage peticion)
    {
        var canal = new FakeChannel();
        var procesador = CrearProcesador(inspector, canal);

        procesador.Enqueue(peticion);

        var enviado = await canal.Esperar(TimeSpan.FromSeconds(5));
        await procesador.StopAsync(CancellationToken.None);

        return enviado;
    }

    private static ServiceQueryProcessor CrearProcesador(FakeInspector inspector, FakeChannel canal)
        => new(inspector, canal, NullLogger<ServiceQueryProcessor>.Instance);

    private sealed class FakeInspector : IServiceInspector
    {
        public Func<string?, ServiceQueryOutcome<IReadOnlyList<ServiceSummary>>>? OnList { get; init; }

        public Func<string, ServiceQueryOutcome<ServiceDetail>>? OnDescribe { get; init; }

        public Func<string, int, DateTimeOffset?, ServiceQueryOutcome<ServiceLogPage>>? OnLogs { get; init; }

        public Func<string, ServiceControlAction, ServiceQueryOutcome<ServiceDetail>>? OnControl { get; init; }

        public ServerPlatform Platform => ServerPlatform.Linux;

        public Task<ServiceQueryOutcome<IReadOnlyList<ServiceSummary>>> ListAsync(string? nameFilter, CancellationToken cancellationToken)
            => Task.FromResult(OnList!(nameFilter));

        public Task<ServiceQueryOutcome<ServiceDetail>> DescribeAsync(string serviceId, CancellationToken cancellationToken)
            => Task.FromResult(OnDescribe!(serviceId));

        public Task<ServiceQueryOutcome<ServiceLogPage>> GetLogsAsync(string serviceId, int maxLines, DateTimeOffset? since, CancellationToken cancellationToken)
            => Task.FromResult(OnLogs!(serviceId, maxLines, since));

        public Task<ServiceQueryOutcome<ServiceDetail>> ControlAsync(string serviceId, ServiceControlAction action, CancellationToken cancellationToken)
            => Task.FromResult(OnControl!(serviceId, action));
    }

    private sealed class FakeChannel : IOrchestratorChannel
    {
        private readonly TaskCompletionSource<AgentToServerMessage> _enviado = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsConnected => true;

        public AgentSessionInfo? Session => null;

        public Task<bool> TrySendAsync(AgentToServerMessage message, CancellationToken cancellationToken)
        {
            _enviado.TrySetResult(message);
            return Task.FromResult(true);
        }

        public Task<AgentToServerMessage> Esperar(TimeSpan tiempo) => _enviado.Task.WaitAsync(tiempo);
    }
}
