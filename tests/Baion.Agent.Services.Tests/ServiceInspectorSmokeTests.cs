using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Agent.Services;
using Baion.Agent.Services.Implementations;
using Baion.Contracts.Enums;
using Baion.Contracts.Messages;
using Baion.Contracts.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Baion.Agent.Services.Tests;

/// <summary>
/// Habla con el gestor de servicios real de la plataforma en la que corren las pruebas. No se simula:
/// lo que se verifica es justamente que las herramientas del sistema se invocan y se interpretan bien.
/// En entornos sin systemd (contenedores) el listado devuelve <c>tool_failed</c> y eso también es válido.
/// </summary>
public class ServiceInspectorSmokeTests
{
    private static IServiceInspector Crear()
    {
        var options = Options.Create(new ServiceInspectionOptions { ToolTimeoutSeconds = 20 });

        return OperatingSystem.IsWindows()
            ? new WindowsServiceInspector(options, NullLogger<WindowsServiceInspector>.Instance)
            : new LinuxServiceInspector(options, NullLogger<LinuxServiceInspector>.Instance);
    }

    [Fact]
    public async Task ListAsync_DevuelveLosServiciosDeLaMaquina()
    {
        var resultado = await Crear().ListAsync(null, CancellationToken.None);

        if (!resultado.IsSuccess)
        {
            Assert.Equal(ServiceQueryFailedMessage.ToolFailedCode, resultado.FailureCode);
            return;
        }

        Assert.NotEmpty(resultado.Value!);
        Assert.All(resultado.Value!, servicio =>
        {
            Assert.False(string.IsNullOrWhiteSpace(servicio.Id));
            Assert.False(string.IsNullOrWhiteSpace(servicio.RawState));
        });
    }

    [Fact]
    public async Task ListAsync_ConFiltro_SoloDevuelveLoQueCoincide()
    {
        var inspector = Crear();
        var todos = await inspector.ListAsync(null, CancellationToken.None);

        if (!todos.IsSuccess || todos.Value!.Count == 0)
        {
            return;
        }

        var muestra = todos.Value![0];
        var fragmento = muestra.Id[..Math.Min(4, muestra.Id.Length)];

        var filtrados = await inspector.ListAsync(fragmento, CancellationToken.None);

        Assert.True(filtrados.IsSuccess);
        Assert.NotEmpty(filtrados.Value!);
        Assert.All(filtrados.Value!, servicio => Assert.Contains(
            fragmento,
            $"{servicio.Id} {servicio.DisplayName}",
            StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DescribeAsync_ConUnServicioReal_DevuelveSuDetalle()
    {
        var inspector = Crear();
        var listado = await inspector.ListAsync(null, CancellationToken.None);

        if (!listado.IsSuccess || listado.Value!.Count == 0)
        {
            return;
        }

        var objetivo = listado.Value![0].Id;
        var detalle = await inspector.DescribeAsync(objetivo, CancellationToken.None);

        Assert.True(detalle.IsSuccess, detalle.FailureMessage);
        Assert.Equal(objetivo, detalle.Value!.Id, ignoreCase: true);
        Assert.False(string.IsNullOrWhiteSpace(detalle.Value!.RawState));
    }

    [Fact]
    public async Task DescribeAsync_ConUnServicioInexistente_DevuelveNotFound()
    {
        var resultado = await Crear().DescribeAsync("baion-servicio-que-no-existe-999", CancellationToken.None);

        Assert.False(resultado.IsSuccess);
        Assert.Equal(ServiceQueryFailedMessage.NotFoundCode, resultado.FailureCode);
    }

    [Fact]
    public async Task GetLogsAsync_ConUnServicioReal_DevuelveUnaPaginaAunqueVacia()
    {
        var inspector = Crear();
        var listado = await inspector.ListAsync(null, CancellationToken.None);

        if (!listado.IsSuccess || listado.Value!.Count == 0)
        {
            return;
        }

        var resultado = await inspector.GetLogsAsync(listado.Value![0].Id, 20, null, CancellationToken.None);

        // O bien logros legibles, o bien un fallo clasificado; nunca una excepción.
        if (resultado.IsSuccess)
        {
            Assert.NotNull(resultado.Value!.Lines);
        }
        else
        {
            Assert.Contains(resultado.FailureCode, new[]
            {
                ServiceQueryFailedMessage.ToolFailedCode,
                ServiceQueryFailedMessage.ForbiddenCode,
                ServiceQueryFailedMessage.NotFoundCode
            });
        }
    }
}
