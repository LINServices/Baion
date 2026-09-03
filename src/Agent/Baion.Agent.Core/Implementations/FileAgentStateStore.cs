using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Agent.Core.Implementations;

internal class FileAgentStateStore(IOptions<AgentOptions> options, ILogger<FileAgentStateStore> logger) : IAgentStateStore
{
    public async Task<AgentState> LoadAsync(CancellationToken cancellationToken)
    {
        var path = ResolveStatePath();

        if (!File.Exists(path))
        {
            return new AgentState { MachineId = ResolveMachineId() };
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var state = await JsonSerializer.DeserializeAsync<AgentState>(stream, JsonOptions, cancellationToken);

            if (state is not null && !string.IsNullOrWhiteSpace(state.MachineId))
            {
                return state;
            }

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

    private string ResolveStatePath() => Path.Combine(ResolveStateDirectory(), StateFileName);

    private string ResolveStateDirectory()
    {
        var configured = options.Value.StateDirectory;

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Baion", "Agent")
            : "/var/lib/baion-agent";
    }

    /// <summary>
    /// En Linux se prefiere <c>/etc/machine-id</c>, que sobrevive a reinstalar el agente. Si no está
    /// disponible se genera uno y se persiste: perderlo solo implica que la máquina se reenrola como nueva.
    /// </summary>
    private string ResolveMachineId()
    {
        if (OperatingSystem.IsLinux())
        {
            foreach (var candidate in LinuxMachineIdPaths)
            {
                try
                {
                    var value = File.ReadAllText(candidate).Trim();

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
                catch (IOException)
                {
                    // Se prueba la siguiente ruta.
                }
                catch (UnauthorizedAccessException)
                {
                    // Se prueba la siguiente ruta.
                }
            }
        }

        logger.LogInformation("No se pudo leer un identificador de máquina del sistema; se genera uno propio");
        return Guid.NewGuid().ToString("N");
    }

    private static void RestrictPermissions(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private static readonly string[] LinuxMachineIdPaths = ["/etc/machine-id", "/var/lib/dbus/machine-id"];

    private const string StateFileName = "agent-state.json";
}
