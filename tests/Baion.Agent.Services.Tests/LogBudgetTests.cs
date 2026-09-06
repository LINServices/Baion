using System;
using System.Collections.Generic;
using System.Linq;
using Baion.Agent.Services.Implementations;
using Baion.Contracts.Services;
using Xunit;

namespace Baion.Agent.Services.Tests;

public class LogBudgetTests
{
    [Fact]
    public void Fit_CuandoCabeEnElPresupuesto_DevuelveLasMismasLineas()
    {
        var lineas = Generar(10, "linea corta");

        var (resultado, truncado) = LogBudget.Fit(lineas, maxBytes: 100_000, alreadyTruncated: false);

        Assert.Same(lineas, resultado);
        Assert.False(truncado);
    }

    [Fact]
    public void Fit_CuandoYaVeniaTruncado_ConservaLaMarcaAunqueQuepa()
    {
        var lineas = Generar(3, "x");

        var (_, truncado) = LogBudget.Fit(lineas, maxBytes: 100_000, alreadyTruncated: true);

        Assert.True(truncado);
    }

    [Fact]
    public void Fit_CuandoSePasaDelPresupuesto_DescartaLasMasAntiguasYMarcaTruncado()
    {
        var lineas = Enumerable.Range(0, 50)
            .Select(indice => new ServiceLogLine(DateTimeOffset.UnixEpoch.AddSeconds(indice), "info", new string('a', 500)))
            .ToList();

        var (resultado, truncado) = LogBudget.Fit(lineas, maxBytes: 4_000, alreadyTruncated: false);

        Assert.True(truncado);
        Assert.NotEmpty(resultado);
        Assert.True(resultado.Count < lineas.Count);

        // Lo que sobrevive es la cola: la última línea original sigue estando.
        Assert.Equal(lineas[^1].Message, resultado[^1].Message);
        Assert.Equal(lineas[^1].Timestamp, resultado[^1].Timestamp);
    }

    [Fact]
    public void Fit_CuandoUnaSolaLineaYaExcedeElPresupuesto_DevuelveEsaLinea()
    {
        var lineas = new List<ServiceLogLine> { new(null, null, new string('z', 10_000)) };

        var (resultado, truncado) = LogBudget.Fit(lineas, maxBytes: 100, alreadyTruncated: false);

        Assert.Single(resultado);
        Assert.True(truncado);
    }

    private static IReadOnlyList<ServiceLogLine> Generar(int cantidad, string mensaje)
        => Enumerable.Range(0, cantidad).Select(_ => new ServiceLogLine(DateTimeOffset.UtcNow, "info", mensaje)).ToList();
}
