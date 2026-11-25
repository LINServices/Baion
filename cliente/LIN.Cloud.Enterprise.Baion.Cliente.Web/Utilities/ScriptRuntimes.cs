using System.Collections.Generic;
using System.Linq;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scripts;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Utilities;

// Duplica ScriptRuntimeCompatibility del orquestador a propósito: el panel no comparte ensamblados
// con el servidor. Solo evita ofrecer destinos que se van a rechazar; quien decide es la API.
public static class ScriptRuntimes
{
    private const string Linux = "linux";

    private const string Windows = "windows";

    public static IReadOnlyList<ScriptRuntimeOption> All { get; } =
    [
        new("bash", "Bash", [Linux]),
        new("sh", "sh", [Linux]),
        new("powerShellCore", "PowerShell Core", [Linux, Windows]),
        new("windowsPowerShell", "Windows PowerShell", [Windows]),
        new("pythonCross", "Python", [Linux, Windows])
    ];

    public static bool IsSupported(string runtime, string platform)
    {
        return All.FirstOrDefault(option => option.Value == runtime)?.Platforms.Contains(platform) ?? false;
    }

    public static string Label(string runtime)
    {
        return All.FirstOrDefault(option => option.Value == runtime)?.Label ?? runtime;
    }

    public static string SupportedPlatforms(string runtime)
    {
        return string.Join(" y ", All.FirstOrDefault(option => option.Value == runtime)?.Platforms.Select(PlatformLabel) ?? []);
    }

    public static string PlatformLabel(string platform)
    {
        return platform switch
        {
            Linux => "Linux",
            Windows => "Windows",
            _ => platform
        };
    }
}
