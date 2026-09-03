using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Baion.Agent.Core;
using Baion.Agent.Core.Implementations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Baion.Agent.Core.Tests;

public class AgentStateStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"baion-agent-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task LoadAsync_SinEstadoPrevio_GeneraIdentificadorDeMaquinaYNoQuedaEnrolado()
    {
        var estado = await CrearStore().LoadAsync(CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(estado.MachineId));
        Assert.False(estado.IsEnrolled);
    }

    [Fact]
    public async Task SaveAsync_YLuegoLoadAsync_DevuelveLaCredencialGuardada()
    {
        var store = CrearStore();
        var original = (await store.LoadAsync(CancellationToken.None)) with { ServerId = Guid.CreateVersion7(), AgentToken = "credencial-de-prueba" };

        await store.SaveAsync(original, CancellationToken.None);
        var recuperado = await CrearStore().LoadAsync(CancellationToken.None);

        Assert.Equal(original.MachineId, recuperado.MachineId);
        Assert.Equal(original.ServerId, recuperado.ServerId);
        Assert.Equal(original.AgentToken, recuperado.AgentToken);
        Assert.True(recuperado.IsEnrolled);
    }

    [Fact]
    public async Task LoadAsync_ConElArchivoCorrupto_SeRegeneraEnLugarDeReventar()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(Path.Combine(_directory, "agent-state.json"), "{ esto no es json");

        var estado = await CrearStore().LoadAsync(CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(estado.MachineId));
        Assert.False(estado.IsEnrolled);
    }

    [Fact]
    public async Task SaveAsync_NoDejaArchivosTemporalesTrasEscribir()
    {
        var store = CrearStore();
        var estado = await store.LoadAsync(CancellationToken.None);

        await store.SaveAsync(estado with { ServerId = Guid.CreateVersion7(), AgentToken = "otra" }, CancellationToken.None);

        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private IAgentStateStore CrearStore() => new FileAgentStateStore(Options.Create(new AgentOptions { StateDirectory = _directory }), NullLogger<FileAgentStateStore>.Instance);
}
