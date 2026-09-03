using System;
using System.Collections.Generic;
using System.Text.Json;
using Baion.Contracts.Enums;
using Baion.Contracts.Messages;
using Baion.Contracts.Metrics;
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
}
