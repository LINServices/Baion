using System.Threading.Tasks;
using Xunit;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Support;

// Dos instancias del orquestador sobre la misma base y el mismo RabbitMQ. Se levantan una sola vez para
// toda la clase: arrancar aplicaciones completas con su base es caro y no hace falta repetirlo por prueba.
// Sin broker no se levanta nada, porque las pruebas que las usan quedan omitidas de todos modos.
public class MultiInstanceFixture : IAsyncLifetime
{
    public OrchestratorFactory InstanciaA { get; private set; } = null!;

    public OrchestratorFactory InstanciaB { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (!RabbitMqProbe.IsReachable())
            return;

        InstanciaA = new OrchestratorFactory(databaseName: null, enableRabbitMq: true);
        await InstanciaA.InitializeAsync();

        InstanciaB = new OrchestratorFactory(InstanciaA.DatabaseName, enableRabbitMq: true);
        await InstanciaB.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (InstanciaB is not null)
            await InstanciaB.DisposeAsync();

        if (InstanciaA is not null)
            await InstanciaA.DisposeAsync();
    }
}
