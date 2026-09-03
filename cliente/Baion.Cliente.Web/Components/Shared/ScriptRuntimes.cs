using System.Collections.Generic;
using System.Linq;

namespace Baion.Cliente.Web.Components.Shared;

/// <summary>Intérprete disponible y las plataformas donde puede correr.</summary>
public record ScriptRuntimeOption(string Value, string Label, IReadOnlyList<string> Platforms);

/// <summary>
/// Catálogo de intérpretes con su compatibilidad. Duplica lo que el orquestador tiene en
/// <c>ScriptRuntimeCompatibility</c>, y es a propósito: el panel es una aplicación aparte y no comparte
/// ensamblados con el servidor. Aquí solo sirve para no ofrecer destinos que se van a rechazar; quien
/// decide de verdad sigue siendo la API, que lo vuelve a comprobar.
/// </summary>
public static class ScriptRuntimes
{
    public static IReadOnlyList<ScriptRuntimeOption> All { get; } =
    [
        new("bash", "Bash", [Linux]),
        new("sh", "sh", [Linux]),
        new("powerShellCore", "PowerShell Core", [Linux, Windows]),
        new("windowsPowerShell", "Windows PowerShell", [Windows]),
        new("pythonCross", "Python", [Linux, Windows])
    ];

    /// <summary>Indica si la plataforma puede ejecutar el intérprete. Un intérprete desconocido no se admite.</summary>
    public static bool IsSupported(string runtime, string platform) => All.FirstOrDefault(option => option.Value == runtime)?.Platforms.Contains(platform) ?? false;

    /// <summary>Etiqueta legible del intérprete; si no se reconoce, se devuelve tal cual.</summary>
    public static string Label(string runtime) => All.FirstOrDefault(option => option.Value == runtime)?.Label ?? runtime;

    /// <summary>Plataformas donde corre el intérprete, ya con nombre legible.</summary>
    public static string SupportedPlatforms(string runtime) => string.Join(" y ", All.FirstOrDefault(option => option.Value == runtime)?.Platforms.Select(PlatformLabel) ?? []);

    public static string PlatformLabel(string platform) => platform switch
    {
        Linux => "Linux",
        Windows => "Windows",
        _ => platform
    };

    private const string Linux = "linux";

    private const string Windows = "windows";
}
