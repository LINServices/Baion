using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Agent.Services;
using LIN.Cloud.Enterprise.Baion.Agent.Services.Implementations;
using LIN.Cloud.Enterprise.Baion.Agent.Services.Tests.Fakes;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Contracts.Messages;
using LIN.Cloud.Enterprise.Baion.Contracts.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LIN.Cloud.Enterprise.Baion.Agent.Services.Tests.Services;

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
    {
        return new ServiceQueryProcessor(inspector, canal, NullLogger<ServiceQueryProcessor>.Instance);
    }
}
