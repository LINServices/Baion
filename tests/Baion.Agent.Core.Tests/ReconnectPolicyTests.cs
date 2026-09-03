using System;
using System.Linq;
using Baion.Agent.Core;
using Baion.Agent.Core.Implementations;
using Microsoft.Extensions.Options;
using Xunit;

namespace Baion.Agent.Core.Tests;

public class ReconnectPolicyTests
{
    [Fact]
    public void GetDelay_NuncaSaleDeLaVentanaConfigurada()
    {
        var policy = CrearPolitica(minimo: 2, maximo: 40);

        var esperas = Enumerable.Range(1, 200).Select(intento => policy.GetDelay(intento).TotalSeconds).ToList();

        Assert.All(esperas, espera => Assert.InRange(espera, 2, 40));
    }

    [Fact]
    public void GetDelay_CreceConLosIntentosHastaTocarElTecho()
    {
        var policy = CrearPolitica(minimo: 1, maximo: 60);

        // Con jitter cada valor es aleatorio, así que se compara el máximo observado por intento.
        var primeros = MaximoObservado(policy, intento: 1);
        var tardios = MaximoObservado(policy, intento: 8);

        Assert.True(tardios > primeros, $"la ventana no creció: {primeros} s frente a {tardios} s");
    }

    [Fact]
    public void GetDelay_ConMuchosIntentos_NoDesbordaNiSePasaDelTecho()
    {
        var policy = CrearPolitica(minimo: 1, maximo: 30);

        var espera = policy.GetDelay(int.MaxValue);

        Assert.InRange(espera.TotalSeconds, 1, 30);
    }

    [Fact]
    public void GetDelay_ReparteLasEsperas_ParaQueLosAgentesNoVuelvanTodosALaVez()
    {
        var policy = CrearPolitica(minimo: 1, maximo: 60);

        var distintas = Enumerable.Range(0, 50).Select(_ => policy.GetDelay(6)).Distinct().Count();

        Assert.True(distintas > 1, "todas las esperas salieron iguales: no hay jitter");
    }

    [Fact]
    public void GetDelay_ConUnaVentanaInvertida_NoDevuelveEsperasNegativas()
    {
        var policy = CrearPolitica(minimo: 30, maximo: 5);

        var espera = policy.GetDelay(3);

        Assert.True(espera >= TimeSpan.Zero);
    }

    private static double MaximoObservado(IReconnectPolicy policy, int intento) => Enumerable.Range(0, 200).Max(_ => policy.GetDelay(intento).TotalSeconds);

    private static IReconnectPolicy CrearPolitica(int minimo, int maximo) => new ExponentialBackoffReconnectPolicy(Options.Create(new AgentOptions { MinReconnectSeconds = minimo, MaxReconnectSeconds = maximo }));
}
