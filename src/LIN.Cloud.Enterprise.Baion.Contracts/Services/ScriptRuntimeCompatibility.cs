using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Contracts;

// Vive en los contratos porque la aplican los dos extremos: el orquestador para rechazar el despacho y el agente para no fiarse de él
public static class ScriptRuntimeCompatibility
{
    private static readonly IReadOnlyDictionary<ScriptRuntime, IReadOnlySet<ServerPlatform>> SupportedPlatforms = new Dictionary<ScriptRuntime, IReadOnlySet<ServerPlatform>>
    {
        [ScriptRuntime.Bash] = new HashSet<ServerPlatform> { ServerPlatform.Linux },
        [ScriptRuntime.Sh] = new HashSet<ServerPlatform> { ServerPlatform.Linux },
        [ScriptRuntime.PowerShellCore] = new HashSet<ServerPlatform> { ServerPlatform.Linux, ServerPlatform.Windows },
        [ScriptRuntime.WindowsPowerShell] = new HashSet<ServerPlatform> { ServerPlatform.Windows },
        [ScriptRuntime.PythonCross] = new HashSet<ServerPlatform> { ServerPlatform.Linux, ServerPlatform.Windows }
    };

    public static bool IsSupported(ScriptRuntime runtime, ServerPlatform platform)
    {
        return SupportedPlatforms.TryGetValue(runtime, out var platforms) && platforms.Contains(platform);
    }
}
