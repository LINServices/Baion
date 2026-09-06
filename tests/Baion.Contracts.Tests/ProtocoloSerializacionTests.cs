using System;
using System.Collections.Generic;
using System.Text.Json;
using Baion.Contracts.Enums;
using Baion.Contracts.Messages;
using Baion.Contracts.Metrics;
using Baion.Contracts.Services;
using Xunit;

namespace Baion.Contracts.Tests;

public class ProtocoloSerializacionTests
{
    [Fact]
    public void ExecuteScriptMessage_RoundTrip_ConservaElContenido()
    {
        var original = new ExecuteScriptMessage(Guid.CreateVersion7(), "echo hola", "sha256:abc", ScriptRuntime.Bash, ExecutionMode.Attached, 30, "/tmp", new Dictionary<string, string> { ["ENTORNO"] = "qa" });

        var json = JsonSerializer.Serialize<ServerToAgentMessage>(original, BaionProtocol.JsonOptions);
        var deserializado = JsonSerializer.Deserialize<ServerToAgentMessage>(json, BaionProtocol.JsonOptions);

        var mensaje = Assert.IsType<ExecuteScriptMessage>(deserializado);
        Assert.Equal(original.ExecutionId, mensaje.ExecutionId);
        Assert.Equal(original.ScriptContent, mensaje.ScriptContent);
        Assert.Equal(original.Runtime, mensaje.Runtime);
        Assert.Equal(original.Mode, mensaje.Mode);
        Assert.Equal(original.MessageId, mensaje.MessageId);
        Assert.Equal("qa", mensaje.EnvironmentVariables!["ENTORNO"]);
    }

    [Fact]
    public void ForceUpdateMessage_RoundTrip_ConservaElContenido()
    {
        var original = new ForceUpdateMessage("2026.9.1", $"https://cdn.baion/agent/{ForceUpdateMessage.RidPlaceholder}.zip", null);

        var json = JsonSerializer.Serialize<ServerToAgentMessage>(original, BaionProtocol.JsonOptions);
        var deserializado = JsonSerializer.Deserialize<ServerToAgentMessage>(json, BaionProtocol.JsonOptions);

        var mensaje = Assert.IsType<ForceUpdateMessage>(deserializado);
        Assert.Equal(original.TargetVersion, mensaje.TargetVersion);
        Assert.Contains(ForceUpdateMessage.RidPlaceholder, mensaje.DownloadUrlTemplate);
        Assert.Null(mensaje.ExpectedChecksum);
    }

    [Fact]
    public void MetricsReportMessage_RoundTrip_ConservaElContenido()
    {
        var original = new MetricsReportMessage(DateTimeOffset.UtcNow, new CpuMetrics(42.5, 8, 1.25), new MemoryMetrics(16_000_000_000, 4_000_000_000), [new DiskMetrics("sda1", "/", 500_000_000_000, 125_000_000_000)]);

        var json = JsonSerializer.Serialize<AgentToServerMessage>(original, BaionProtocol.JsonOptions);
        var deserializado = JsonSerializer.Deserialize<AgentToServerMessage>(json, BaionProtocol.JsonOptions);

        var mensaje = Assert.IsType<MetricsReportMessage>(deserializado);
        Assert.Equal(42.5, mensaje.Cpu.UsagePercent);
        Assert.Equal(75d, mensaje.Memory.UsagePercent);
        Assert.Equal("/", Assert.Single(mensaje.Disks).MountPoint);
    }

    [Fact]
    public void Serializacion_UsaDiscriminadorYEnumsEnTexto()
    {
        var mensaje = new ExecuteScriptMessage(Guid.CreateVersion7(), "Get-Date", "sha256:abc", ScriptRuntime.PowerShellCore, ExecutionMode.Detached, 30, null, null);

        var json = JsonSerializer.Serialize<ServerToAgentMessage>(mensaje, BaionProtocol.JsonOptions);

        Assert.Contains($"\"type\":\"{ExecuteScriptMessage.TypeDiscriminator}\"", json);
        Assert.Contains("\"runtime\":\"powerShellCore\"", json);
        Assert.Contains("\"mode\":\"detached\"", json);
        Assert.DoesNotContain("workingDirectory", json);
        Assert.DoesNotContain("usedBytes", json);
    }

    [Fact]
    public void ListServicesRequestMessage_RoundTrip_ConservaElContenido()
    {
        var original = new ListServicesRequestMessage(Guid.CreateVersion7(), "ngin");

        var json = JsonSerializer.Serialize<ServerToAgentMessage>(original, BaionProtocol.JsonOptions);
        var deserializado = JsonSerializer.Deserialize<ServerToAgentMessage>(json, BaionProtocol.JsonOptions);

        var mensaje = Assert.IsType<ListServicesRequestMessage>(deserializado);
        Assert.Equal(original.RequestId, mensaje.RequestId);
        Assert.Equal("ngin", mensaje.NameFilter);
        Assert.Contains($"\"type\":\"{ListServicesRequestMessage.TypeDiscriminator}\"", json);
    }

    [Fact]
    public void ServiceLogsRequestMessage_RoundTrip_ConservaElContenido()
    {
        var desde = DateTimeOffset.UtcNow.AddMinutes(-30);
        var original = new ServiceLogsRequestMessage(Guid.CreateVersion7(), "nginx.service", 500, desde);

        var json = JsonSerializer.Serialize<ServerToAgentMessage>(original, BaionProtocol.JsonOptions);
        var deserializado = JsonSerializer.Deserialize<ServerToAgentMessage>(json, BaionProtocol.JsonOptions);

        var mensaje = Assert.IsType<ServiceLogsRequestMessage>(deserializado);
        Assert.Equal(original.RequestId, mensaje.RequestId);
        Assert.Equal("nginx.service", mensaje.ServiceId);
        Assert.Equal(500, mensaje.MaxLines);
        Assert.Equal(desde, mensaje.Since);
    }

    [Fact]
    public void ControlServiceRequestMessage_RoundTrip_ConservaLaAccion()
    {
        var original = new ControlServiceRequestMessage(Guid.CreateVersion7(), "Spooler", ServiceControlAction.Restart);

        var json = JsonSerializer.Serialize<ServerToAgentMessage>(original, BaionProtocol.JsonOptions);
        var deserializado = JsonSerializer.Deserialize<ServerToAgentMessage>(json, BaionProtocol.JsonOptions);

        var mensaje = Assert.IsType<ControlServiceRequestMessage>(deserializado);
        Assert.Equal(ServiceControlAction.Restart, mensaje.Action);
        Assert.Contains("\"action\":\"restart\"", json);
    }

    [Fact]
    public void ServicesListedMessage_RoundTrip_ConservaLosServicios()
    {
        var original = new ServicesListedMessage(Guid.CreateVersion7(),
        [
            new ServiceSummary("nginx.service", "Servidor web nginx", ServiceRuntimeState.Running, "active (running)", ServiceStartupMode.Automatic),
            new ServiceSummary("cron.service", "Demonio cron", ServiceRuntimeState.Stopped, "inactive (dead)", ServiceStartupMode.Manual),
        ]);

        var json = JsonSerializer.Serialize<AgentToServerMessage>(original, BaionProtocol.JsonOptions);
        var deserializado = JsonSerializer.Deserialize<AgentToServerMessage>(json, BaionProtocol.JsonOptions);

        var mensaje = Assert.IsType<ServicesListedMessage>(deserializado);
        Assert.Equal(original.RequestId, mensaje.RequestId);
        Assert.Equal(2, mensaje.Services.Count);
        Assert.Equal("nginx.service", mensaje.Services[0].Id);
        Assert.Equal(ServiceRuntimeState.Running, mensaje.Services[0].State);
        Assert.Contains("\"state\":\"running\"", json);
    }

    [Fact]
    public void ServiceDescribedMessage_RoundTrip_ConservaElDetalle()
    {
        var activo = DateTimeOffset.UtcNow.AddDays(-3);
        var detalle = new ServiceDetail("nginx.service", "Servidor web nginx", "Un servidor HTTP y proxy inverso",
            ServiceRuntimeState.Running, "active (running)", "running", ServiceStartupMode.Automatic,
            1234, activo, 52_428_800, "/lib/systemd/system/nginx.service", null, ["network.target"]);
        var original = new ServiceDescribedMessage(Guid.CreateVersion7(), detalle);

        var json = JsonSerializer.Serialize<AgentToServerMessage>(original, BaionProtocol.JsonOptions);
        var deserializado = JsonSerializer.Deserialize<AgentToServerMessage>(json, BaionProtocol.JsonOptions);

        var mensaje = Assert.IsType<ServiceDescribedMessage>(deserializado);
        Assert.Equal(1234, mensaje.Service.MainProcessId);
        Assert.Equal(activo, mensaje.Service.ActiveSince);
        Assert.Equal(52_428_800, mensaje.Service.MemoryBytes);
        Assert.Equal("network.target", Assert.Single(mensaje.Service.Dependencies));
        Assert.Null(mensaje.Service.ExitCode);
    }

    [Fact]
    public void ServiceLogsMessage_RoundTrip_ConservaLasLineas()
    {
        var t = DateTimeOffset.UtcNow;
        var original = new ServiceLogsMessage(Guid.CreateVersion7(),
        [
            new ServiceLogLine(t, "info", "Arrancando nginx"),
            new ServiceLogLine(null, null, "Línea sin metadatos"),
        ], Truncated: true);

        var json = JsonSerializer.Serialize<AgentToServerMessage>(original, BaionProtocol.JsonOptions);
        var deserializado = JsonSerializer.Deserialize<AgentToServerMessage>(json, BaionProtocol.JsonOptions);

        var mensaje = Assert.IsType<ServiceLogsMessage>(deserializado);
        Assert.True(mensaje.Truncated);
        Assert.Equal(2, mensaje.Lines.Count);
        Assert.Equal("Arrancando nginx", mensaje.Lines[0].Message);
        Assert.Null(mensaje.Lines[1].Timestamp);
    }

    [Fact]
    public void ServiceControlledMessage_RoundTrip_ConservaElDetalle()
    {
        var detalle = new ServiceDetail("Spooler", "Cola de impresión", null,
            ServiceRuntimeState.Running, "Running", null, ServiceStartupMode.Automatic,
            4242, null, null, "C:\\Windows\\System32\\spoolsv.exe", null, []);
        var original = new ServiceControlledMessage(Guid.CreateVersion7(), detalle);

        var json = JsonSerializer.Serialize<AgentToServerMessage>(original, BaionProtocol.JsonOptions);
        var deserializado = JsonSerializer.Deserialize<AgentToServerMessage>(json, BaionProtocol.JsonOptions);

        var mensaje = Assert.IsType<ServiceControlledMessage>(deserializado);
        Assert.Equal("Spooler", mensaje.Service.Id);
        Assert.Equal(ServiceRuntimeState.Running, mensaje.Service.State);
        Assert.Empty(mensaje.Service.Dependencies);
    }

    [Fact]
    public void ServiceQueryFailedMessage_RoundTrip_ConservaElCodigo()
    {
        var original = new ServiceQueryFailedMessage(Guid.CreateVersion7(), ServiceQueryFailedMessage.NotFoundCode, "El servicio no existe.");

        var json = JsonSerializer.Serialize<AgentToServerMessage>(original, BaionProtocol.JsonOptions);
        var deserializado = JsonSerializer.Deserialize<AgentToServerMessage>(json, BaionProtocol.JsonOptions);

        var mensaje = Assert.IsType<ServiceQueryFailedMessage>(deserializado);
        Assert.Equal(ServiceQueryFailedMessage.NotFoundCode, mensaje.Code);
        Assert.Equal("El servicio no existe.", mensaje.Message);
    }
}
