using Baion.Agent.Metrics.Implementations;
using Xunit;

namespace Baion.Agent.Metrics.Tests;

public class CpuSampleTests
{
    [Fact]
    public void UsagePercent_ConMitadDelTiempoOcioso_DevuelveCincuentaPorCiento()
    {
        var uso = CpuSample.UsagePercent(new CpuSample(1000, 500), new CpuSample(2000, 1000));

        Assert.Equal(50, uso);
    }

    [Fact]
    public void UsagePercent_SinTiempoTranscurrido_DevuelveCero()
    {
        var uso = CpuSample.UsagePercent(new CpuSample(1000, 500), new CpuSample(1000, 500));

        Assert.Equal(0, uso);
    }

    [Fact]
    public void UsagePercent_ConContadoresQueRetroceden_DevuelveCeroEnLugarDeUnValorAbsurdo()
    {
        // Pasa si el sistema reinicia sus estadísticas: la ventana no es comparable y se descarta.
        var uso = CpuSample.UsagePercent(new CpuSample(5000, 2000), new CpuSample(1000, 400));

        Assert.Equal(0, uso);
    }

    [Fact]
    public void UsagePercent_ConTodoElTiempoOcioso_DevuelveCero()
    {
        var uso = CpuSample.UsagePercent(new CpuSample(1000, 1000), new CpuSample(2000, 2000));

        Assert.Equal(0, uso);
    }

    [Fact]
    public void UsagePercent_ConTiempoOciosoMayorQueElTotal_SeAcotaAlRango()
    {
        var uso = CpuSample.UsagePercent(new CpuSample(1000, 500), new CpuSample(1100, 900));

        Assert.InRange(uso, 0, 100);
    }
}
