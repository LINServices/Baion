using System;
using System.Text;
using System.Text.Json;
using Baion.Agent.Services.Implementations;
using Baion.Contracts.Enums;
using Xunit;

namespace Baion.Agent.Services.Tests;

public class WindowsServiceParserTests
{
    [Fact]
    public void Encode_ProduceBase64DeUtf16LE()
    {
        var codificado = WindowsServiceParser.Encode("Get-Service");

        Assert.Equal("Get-Service", Encoding.Unicode.GetString(Convert.FromBase64String(codificado)));
    }

    [Fact]
    public void TryParseEnvelope_ConSobreCorrecta_DevuelveOkYDatos()
    {
        const string json = """{"ok":true,"data":[{"name":"Spooler"}]}""";

        Assert.True(WindowsServiceParser.TryParseEnvelope(json, out var sobre));
        Assert.True(sobre.Ok);
        Assert.Equal(JsonValueKind.Array, sobre.Data.ValueKind);
    }

    [Fact]
    public void TryParseEnvelope_ConFalloClasificado_ConservaCodigoYMensaje()
    {
        const string json = """{"ok":false,"code":"forbidden","message":"Access is denied"}""";

        Assert.True(WindowsServiceParser.TryParseEnvelope(json, out var sobre));
        Assert.False(sobre.Ok);
        Assert.Equal(PowerShellEnvelope.ForbiddenCode, sobre.Code);
        Assert.Equal("Access is denied", sobre.Message);
    }

    [Fact]
    public void TryParseEnvelope_ConBasura_Falla()
    {
        Assert.False(WindowsServiceParser.TryParseEnvelope("no soy json", out _));
        Assert.False(WindowsServiceParser.TryParseEnvelope("", out _));
    }

    [Fact]
    public void ParseSummaries_AdmiteArrayYObjetoSuelto()
    {
        var varios = WindowsServiceParser.ParseSummaries(Data("""[{"name":"A","displayName":"Servicio A","state":"Running","startMode":"Auto"},{"name":"B","state":"Stopped","startMode":"Disabled"}]"""));
        Assert.Equal(2, varios.Count);
        Assert.Equal("Servicio A", varios[0].DisplayName);
        Assert.Equal(ServiceRuntimeState.Running, varios[0].State);
        Assert.Equal("B", varios[1].DisplayName);

        var uno = WindowsServiceParser.ParseSummaries(Data("""{"name":"Solo","state":"Running","startMode":"Manual"}"""));
        Assert.Single(uno);
        Assert.Equal("Solo", uno[0].Id);
        Assert.Equal(ServiceStartupMode.Manual, uno[0].StartupMode);
    }

    [Fact]
    public void ParseSummaries_ConservaElEstadoCrudo()
    {
        var resumen = WindowsServiceParser.ParseSummaries(Data("""{"name":"X","state":"Start Pending","startMode":"Auto"}"""));

        Assert.Equal("Start Pending", resumen[0].RawState);
        Assert.Equal(ServiceRuntimeState.Starting, resumen[0].State);
    }

    [Fact]
    public void ParseDetail_LeeLosCamposOpcionales()
    {
        const string json = """
            {
              "name":"Spooler","displayName":"Print Spooler","description":"Administra los trabajos de impresión",
              "state":"Running","startMode":"Auto","processId":444,"pathName":"C:\\Windows\\System32\\spoolsv.exe",
              "startTime":"2026-09-06T10:00:00.0000000Z","workingSet":10485760,"dependencies":["RPCSS","http"]
            }
            """;

        var detalle = WindowsServiceParser.ParseDetail(Data(json), "Spooler");

        Assert.Equal("Spooler", detalle.Id);
        Assert.Equal("Print Spooler", detalle.DisplayName);
        Assert.Equal("Administra los trabajos de impresión", detalle.Description);
        Assert.Equal(ServiceRuntimeState.Running, detalle.State);
        Assert.Equal("Running", detalle.RawState);
        Assert.Null(detalle.SubState);
        Assert.Equal(ServiceStartupMode.Automatic, detalle.StartupMode);
        Assert.Equal(444, detalle.MainProcessId);
        Assert.Equal(@"C:\Windows\System32\spoolsv.exe", detalle.ExecPath);
        Assert.Equal(new DateTimeOffset(2026, 9, 6, 10, 0, 0, TimeSpan.Zero), detalle.ActiveSince);
        Assert.Equal(10485760, detalle.MemoryBytes);
        Assert.Equal(new[] { "RPCSS", "http" }, detalle.Dependencies);
    }

    [Fact]
    public void ParseDetail_ConProcesoDetenido_DejaLosCamposDeProcesoEnNull()
    {
        var detalle = WindowsServiceParser.ParseDetail(Data("""{"name":"X","state":"Stopped","startMode":"Disabled","processId":0}"""), "X");

        Assert.Null(detalle.MainProcessId);
        Assert.Null(detalle.ActiveSince);
        Assert.Null(detalle.MemoryBytes);
        Assert.Empty(detalle.Dependencies);
        Assert.Equal(ServiceRuntimeState.Stopped, detalle.State);
    }

    [Fact]
    public void ParseLogLines_LeeMarcaNivelYMensaje()
    {
        const string json = """[{"timestamp":"2026-09-06T10:00:00.0000000Z","level":"Information","message":"El servicio se inició"}]""";

        var lineas = WindowsServiceParser.ParseLogLines(Data(json));

        Assert.Single(lineas);
        Assert.Equal(new DateTimeOffset(2026, 9, 6, 10, 0, 0, TimeSpan.Zero), lineas[0].Timestamp);
        Assert.Equal("Information", lineas[0].Level);
        Assert.Equal("El servicio se inició", lineas[0].Message);
    }

    [Theory]
    [InlineData("Running", ServiceRuntimeState.Running)]
    [InlineData("Stopped", ServiceRuntimeState.Stopped)]
    [InlineData("Start Pending", ServiceRuntimeState.Starting)]
    [InlineData("Stop Pending", ServiceRuntimeState.Stopping)]
    [InlineData("Paused", ServiceRuntimeState.Stopped)]
    [InlineData("algo raro", ServiceRuntimeState.Unknown)]
    public void MapState_TraduceElEstadoDeWindows(string estado, ServiceRuntimeState esperado)
        => Assert.Equal(esperado, WindowsServiceParser.MapState(estado));

    [Theory]
    [InlineData("Auto", ServiceStartupMode.Automatic)]
    [InlineData("Manual", ServiceStartupMode.Manual)]
    [InlineData("Disabled", ServiceStartupMode.Disabled)]
    [InlineData("Boot", ServiceStartupMode.Automatic)]
    [InlineData("qué", ServiceStartupMode.Unknown)]
    public void MapStartupMode_TraduceElModoDeArranqueDeWindows(string modo, ServiceStartupMode esperado)
        => Assert.Equal(esperado, WindowsServiceParser.MapStartupMode(modo));

    private static JsonElement Data(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
