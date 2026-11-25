using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LIN.Cloud.Enterprise.Baion.Agent.Core.Implementations;

internal class FileAgentStateStore(IOptions<AgentOptions> options, ILogger<FileAgentStateStore> logger) : IAgentStateStore
{
    private const string StateFileName = "agent-state.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private static readonly string[] LinuxMachineIdPaths = ["/etc/machine-id", "/var/lib/dbus/machine-id"];

    public async Task<AgentState> LoadAsync(CancellationToken cancellationToken)
    {
        var path = ResolveStatePath();

        if (!File.Exists(path))
            return new AgentState { MachineId = ResolveMachineId() };

        try
        {
            await using var stream = File.OpenRead(path);
            var state = await JsonSerializer.DeserializeAsync<AgentState>(stream, JsonOptions, cancellationToken);

            if (state is not null && !string.IsNullOrWhiteSpace(state.MachineId))
                return state;

            logger.LogWarning("El estado guardado en {StatePath} está incompleto; se regenera", path);
        }
        catch (JsonException exception)
        {
            logger.LogWarning("No se pudo leer el estado guardado en {StatePath}: {Motivo}. Se regenera.", path, exception.Message);
        }

        return new AgentState { MachineId = ResolveMachineId() };
    }

    public async Task SaveAsync(AgentState state, CancellationToken cancellationToken)
    {
        var path = ResolveStatePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Escritura atómica: si el proceso muere a media escritura, el archivo anterior sigue intacto.
        var temporary = $"{path}.tmp";

        await using (var stream = File.Create(temporary))
        {
            await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken);
        }

        RestrictPermissions(temporary);
        File.Move(temporary, path, overwrite: true);
    }

    private string ResolveStatePath()
    {
        return Path.Combine(ResolveStateDirectory(), StateFileName);
    }

    private string ResolveStateDirectory()
    {
        var configured = options.Value.StateDirectory;

        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Baion", "Agent")
            : "/var/lib/baion-agent";
    }

    // En Linux se prefiere /etc/machine-id, que sobrevive a reinstalar el agente. Si no está
    // disponible se genera uno y se persiste: perderlo solo implica que la máquina se reenrola como nueva.
    private string ResolveMachineId()
    {
        var systemId = OperatingSystem.IsLinux()
            ? LinuxMachineIdPaths.Select(ReadMachineId).FirstOrDefault(id => id is not null)
            : null;

        if (systemId is not null)
            return systemId;

        logger.LogInformation("No se pudo leer un identificador de máquina del sistema; se genera uno propio");
        return Guid.NewGuid().ToString("N");
    }

    private static string? ReadMachineId(string path)
    {
        try
        {
            var value = File.ReadAllText(path).Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static void RestrictPermissions(string path)
    {
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}
