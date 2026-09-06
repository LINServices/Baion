using System;
using System.Linq;
using Baion.Agent.Services.Implementations;
using Baion.Contracts.Enums;
using Xunit;

namespace Baion.Agent.Services.Tests;

public class SystemdParserTests
{
    [Fact]
    public void ParseListUnits_LeeNombreEstadoYDescripcion()
    {
        const string json = """
            [
              {"unit":"nginx.service","load":"loaded","active":"active","sub":"running","description":"A high performance web server"},
              {"unit":"cron.service","load":"loaded","active":"inactive","sub":"dead","description":"Regular background program processing daemon"}
            ]
            """;

        var unidades = SystemdParser.ParseListUnits(json);

        Assert.Equal(2, unidades.Count);
        Assert.Equal("nginx.service", unidades[0].Unit);
        Assert.Equal("active", unidades[0].Active);
        Assert.Equal("running", unidades[0].Sub);
        Assert.Equal("A high performance web server", unidades[0].Description);
    }

    [Fact]
    public void ParseUnitFileStates_MapeaUnidadAEstadoDeArranque()
    {
        const string json = """
            [
              {"unit_file":"nginx.service","state":"enabled","preset":"enabled"},
              {"unit_file":"cron.service","state":"disabled","preset":"enabled"}
            ]
            """;

        var estados = SystemdParser.ParseUnitFileStates(json);

        Assert.Equal("enabled", estados["nginx.service"]);
        Assert.Equal("disabled", estados["cron.service"]);
    }

    [Fact]
    public void ParseShow_ParteLasLineasClaveValor()
    {
        const string salida = "Id=nginx.service\nDescription=web server\nActiveState=active\nSubState=running\nMainPID=1234\n";

        var propiedades = SystemdParser.ParseShow(salida);

        Assert.Equal("nginx.service", propiedades["Id"]);
        Assert.Equal("web server", propiedades["Description"]);
        Assert.Equal("1234", propiedades["MainPID"]);
    }

    [Fact]
    public void ParseShow_AdmiteSignosIgualEnElValor()
    {
        var propiedades = SystemdParser.ParseShow("Environment=FOO=bar BAZ=qux\n");

        Assert.Equal("FOO=bar BAZ=qux", propiedades["Environment"]);
    }

    [Fact]
    public void ParseJournal_LeeCadaLineaJsonComoUnRegistro()
    {
        var salida = string.Join('\n',
            """{"__REALTIME_TIMESTAMP":"1700000000000000","PRIORITY":"6","MESSAGE":"arrancando"}""",
            """{"__REALTIME_TIMESTAMP":"1700000001000000","PRIORITY":"3","MESSAGE":"algo falló"}""");

        var lineas = SystemdParser.ParseJournal(salida);

        Assert.Equal(2, lineas.Count);
        Assert.Equal("info", lineas[0].Level);
        Assert.Equal("arrancando", lineas[0].Message);
        Assert.Equal("error", lineas[1].Level);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000), lineas[0].Timestamp);
    }

    [Fact]
    public void ParseJournal_ConMensajeComoArrayDeBytes_LoDecodificaComoUtf8()
    {
        // "hi" = 104, 105
        var lineas = SystemdParser.ParseJournal("""{"MESSAGE":[104,105]}""");

        Assert.Single(lineas);
        Assert.Equal("hi", lineas[0].Message);
    }

    [Fact]
    public void ParseJournal_SaltaLineasQueNoSonJson()
    {
        var lineas = SystemdParser.ParseJournal("-- Logs begin --\n" + """{"MESSAGE":"ok"}""" + "\nbasura");

        Assert.Single(lineas);
        Assert.Equal("ok", lineas[0].Message);
    }

    [Theory]
    [InlineData("active", ServiceRuntimeState.Running)]
    [InlineData("activating", ServiceRuntimeState.Starting)]
    [InlineData("deactivating", ServiceRuntimeState.Stopping)]
    [InlineData("inactive", ServiceRuntimeState.Stopped)]
    [InlineData("failed", ServiceRuntimeState.Failed)]
    [InlineData("rarunknown", ServiceRuntimeState.Unknown)]
    [InlineData(null, ServiceRuntimeState.Unknown)]
    public void MapRuntimeState_TraduceElEstadoDeSystemd(string? activeState, ServiceRuntimeState esperado)
        => Assert.Equal(esperado, SystemdParser.MapRuntimeState(activeState));

    [Theory]
    [InlineData("enabled", ServiceStartupMode.Automatic)]
    [InlineData("enabled-runtime", ServiceStartupMode.Automatic)]
    [InlineData("disabled", ServiceStartupMode.Disabled)]
    [InlineData("masked", ServiceStartupMode.Disabled)]
    [InlineData("static", ServiceStartupMode.Manual)]
    [InlineData("", ServiceStartupMode.Unknown)]
    [InlineData(null, ServiceStartupMode.Unknown)]
    public void MapStartupMode_TraduceElEstadoDelArchivoDeUnidad(string? unitFileState, ServiceStartupMode esperado)
        => Assert.Equal(esperado, SystemdParser.MapStartupMode(unitFileState));

    [Fact]
    public void BuildRawState_CombinaEstadoYSubestado()
    {
        Assert.Equal("active (running)", SystemdParser.BuildRawState("active", "running"));
        Assert.Equal("inactive", SystemdParser.BuildRawState("inactive", ""));
        Assert.Equal("unknown", SystemdParser.BuildRawState(null, null));
    }

    [Fact]
    public void ParseTimestamp_LeeElFormatoDeSystemdConDiaDeLaSemanaYZona()
    {
        var resultado = SystemdParser.ParseTimestamp("Sat 2026-09-06 12:34:56 UTC");

        Assert.Equal(new DateTimeOffset(2026, 9, 6, 12, 34, 56, TimeSpan.Zero), resultado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("n/a")]
    [InlineData("0")]
    public void ParseTimestamp_DevuelveNullCuandoNoHayMarca(string valor)
        => Assert.Null(SystemdParser.ParseTimestamp(valor));

    [Fact]
    public void ParseMemory_DevuelveNullCuandoLaContabilidadEstaApagada()
    {
        Assert.Null(SystemdParser.ParseMemory("[not set]"));
        Assert.Null(SystemdParser.ParseMemory("18446744073709551615"));
        Assert.Equal(2048, SystemdParser.ParseMemory("2048"));
    }

    [Fact]
    public void ParsePositiveInt_DescartaCeroYNoNumeros()
    {
        Assert.Null(SystemdParser.ParsePositiveInt("0"));
        Assert.Null(SystemdParser.ParsePositiveInt("x"));
        Assert.Equal(17, SystemdParser.ParsePositiveInt("17"));
    }

    [Fact]
    public void ParseExitCode_SoloTieneSentidoSiElServicioNoEstaCorriendo()
    {
        Assert.Null(SystemdParser.ParseExitCode("0", "active"));
        Assert.Equal(1, SystemdParser.ParseExitCode("1", "failed"));
    }

    [Fact]
    public void ParseDependencies_SoloDevuelveOtrasUnidadesDeServicio()
    {
        var dependencias = SystemdParser.ParseDependencies("sysinit.target postgresql.service -.mount redis.service");

        Assert.Equal(new[] { "postgresql.service", "redis.service" }, dependencias.ToArray());
    }
}
