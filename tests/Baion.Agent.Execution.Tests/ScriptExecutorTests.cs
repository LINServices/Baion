using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Baion.Agent.Execution;
using Baion.Agent.Execution.Implementations;
using Baion.Contracts.Enums;
using Baion.Contracts.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Baion.Agent.Execution.Tests;

/// <summary>
/// Ejecuta procesos de verdad sobre la plataforma en la que corren las pruebas. No se simula el proceso:
/// lo que hay que verificar es justamente el trato con el sistema operativo.
/// </summary>
public class ScriptExecutorTests : IDisposable
{
    private readonly string _workingRoot = Path.Combine(Path.GetTempPath(), $"baion-exec-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ExecuteAsync_ConScriptCorrecto_DevuelveExitoYSuSalida()
    {
        var salida = new StringBuilder();
        var resultado = await EjecutarAsync(EcoScript("hola baion"), onOutput: (stream, contenido) => salida.Append(contenido));

        Assert.Equal(ExecutionStatus.Succeeded, resultado.Status);
        Assert.Equal(0, resultado.ExitCode);
        Assert.Contains("hola baion", salida.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_ConCodigoDeSalidaDistintoDeCero_MarcaFallo()
    {
        var resultado = await EjecutarAsync(ExitScript(3));

        Assert.Equal(ExecutionStatus.Failed, resultado.Status);
        Assert.Equal(3, resultado.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_ConSalidaPorStderr_LaEntregaEnSuPropioFlujo()
    {
        var flujos = new List<OutputStream>();
        var resultado = await EjecutarAsync(StderrScript("algo salió mal"), onOutput: (stream, _) => flujos.Add(stream));

        Assert.Equal(ExecutionStatus.Succeeded, resultado.Status);
        Assert.Contains(OutputStream.Stderr, flujos);
    }

    [Fact]
    public async Task ExecuteAsync_AlVencerElTimeout_TerminaElProcesoYMarcaTimedOut()
    {
        var cronometro = Stopwatch.StartNew();
        var resultado = await EjecutarAsync(DormirScript(60), timeoutSeconds: 2);
        cronometro.Stop();

        Assert.Equal(ExecutionStatus.TimedOut, resultado.Status);
        Assert.Null(resultado.ExitCode);
        Assert.True(cronometro.Elapsed < TimeSpan.FromSeconds(30), $"no se cortó a tiempo: tardó {cronometro.Elapsed}");
    }

    [Fact]
    public async Task ExecuteAsync_ConChecksumQueNoCuadra_NiSiquieraEscribeElScript()
    {
        var request = NuevaOrden(EcoScript("no debería correr")) with { ScriptChecksum = new string('0', 64) };
        var resultado = await CrearEjecutor().ExecuteAsync(request, _ => Task.CompletedTask, (_, _) => Task.CompletedTask, CancellationToken.None);

        Assert.Equal(ExecutionStatus.Failed, resultado.Status);
        Assert.Contains("checksum", resultado.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_ConIntérpreteQueLaPlataformaNoSoporta_LoRechaza()
    {
        // En Windows nunca hay bash; en Linux nunca hay Windows PowerShell.
        var incompatible = OperatingSystem.IsWindows() ? ScriptRuntime.Bash : ScriptRuntime.WindowsPowerShell;
        var request = NuevaOrden("echo hola") with { Runtime = incompatible };

        var resultado = await CrearEjecutor().ExecuteAsync(request, _ => Task.CompletedTask, (_, _) => Task.CompletedTask, CancellationToken.None);

        Assert.Equal(ExecutionStatus.Failed, resultado.Status);
        Assert.Contains(incompatible.ToString(), resultado.ErrorMessage!);
    }

    [Fact]
    public async Task ExecuteAsync_EnModoDetached_VuelveSinEsperarAlProceso()
    {
        var cronometro = Stopwatch.StartNew();
        var resultado = await EjecutarAsync(DormirScript(30), mode: ExecutionMode.Detached, timeoutSeconds: 120);
        cronometro.Stop();

        Assert.Equal(ExecutionStatus.Succeeded, resultado.Status);

        // Lanzamiento correcto sin código de salida: el agente no llega a observarlo.
        Assert.Null(resultado.ExitCode);
        Assert.True(cronometro.Elapsed < TimeSpan.FromSeconds(10), $"esperó al proceso: tardó {cronometro.Elapsed}");
    }

    [Fact]
    public async Task ExecuteAsync_ConVariablesDeEntorno_LasPasaAlProceso()
    {
        var salida = new StringBuilder();
        var request = NuevaOrden(LeerVariableScript("BAION_PRUEBA")) with { EnvironmentVariables = new Dictionary<string, string> { ["BAION_PRUEBA"] = "valor-esperado" } };

        var resultado = await CrearEjecutor().ExecuteAsync(RecalcularChecksum(request), _ => Task.CompletedTask, (_, contenido) =>
        {
            salida.Append(contenido);
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.Equal(ExecutionStatus.Succeeded, resultado.Status);
        Assert.Contains("valor-esperado", salida.ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_workingRoot))
        {
            try
            {
                Directory.Delete(_workingRoot, recursive: true);
            }
            catch (IOException)
            {
                // Un proceso Detached puede seguir vivo; no es motivo para fallar la prueba.
            }
        }

        GC.SuppressFinalize(this);
    }

    private async Task<ScriptExecutionOutcome> EjecutarAsync(string contenido, Action<OutputStream, string>? onOutput = null, ExecutionMode mode = ExecutionMode.Attached, int timeoutSeconds = 60)
    {
        var request = NuevaOrden(contenido) with { Mode = mode, TimeoutSeconds = timeoutSeconds };

        return await CrearEjecutor().ExecuteAsync(request, _ => Task.CompletedTask, (stream, texto) =>
        {
            onOutput?.Invoke(stream, texto);
            return Task.CompletedTask;
        }, CancellationToken.None);
    }

    private IScriptExecutor CrearEjecutor()
    {
        var options = Options.Create(new ScriptExecutionOptions { WorkingRoot = _workingRoot });

        return OperatingSystem.IsWindows()
            ? new WindowsScriptExecutor(options, NullLogger<WindowsScriptExecutor>.Instance)
            : new LinuxScriptExecutor(options, NullLogger<LinuxScriptExecutor>.Instance);
    }

    private static ExecuteScriptMessage NuevaOrden(string contenido) => new(Guid.CreateVersion7(), contenido, Checksum(contenido), PlataformaRuntime, ExecutionMode.Attached, 60, null, null);

    private static ExecuteScriptMessage RecalcularChecksum(ExecuteScriptMessage request) => request with { ScriptChecksum = Checksum(request.ScriptContent) };

    private static ScriptRuntime PlataformaRuntime => OperatingSystem.IsWindows() ? ScriptRuntime.WindowsPowerShell : ScriptRuntime.Bash;

    private static string EcoScript(string mensaje) => OperatingSystem.IsWindows() ? $"Write-Output '{mensaje}'" : $"echo '{mensaje}'";

    private static string ExitScript(int codigo) => OperatingSystem.IsWindows() ? $"exit {codigo}" : $"exit {codigo}";

    private static string StderrScript(string mensaje) => OperatingSystem.IsWindows() ? $"[Console]::Error.WriteLine('{mensaje}')" : $"echo '{mensaje}' >&2";

    private static string DormirScript(int segundos) => OperatingSystem.IsWindows() ? $"Start-Sleep -Seconds {segundos}" : $"sleep {segundos}";

    private static string LeerVariableScript(string nombre) => OperatingSystem.IsWindows() ? $"Write-Output $env:{nombre}" : $"echo ${nombre}";

    private static string Checksum(string contenido) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(contenido)));
}
