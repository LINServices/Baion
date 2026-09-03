using System.Collections.Generic;
using Baion.Contracts.Enums;

namespace Baion.Contracts;

/// <summary>
/// Qué intérpretes puede ejecutar cada plataforma. Vive en los contratos porque los dos extremos la
/// necesitan: el orquestador para rechazar el despacho cuanto antes, y el agente para no fiarse de él.
/// </summary>
public static class ScriptRuntimeCompatibility
{
    /// <summary>Indica si la plataforma puede ejecutar el intérprete indicado.</summary>
    public static bool IsSupported(ScriptRuntime runtime, ServerPlatform platform) => SupportedPlatforms.TryGetValue(runtime, out var platforms) && platforms.Contains(platform);

    /// <summary>Plataformas admitidas por cada intérprete.</summary>
    private static readonly IReadOnlyDictionary<ScriptRuntime, IReadOnlySet<ServerPlatform>> SupportedPlatforms = new Dictionary<ScriptRuntime, IReadOnlySet<ServerPlatform>>
    {
        [ScriptRuntime.Bash] = new HashSet<ServerPlatform> { ServerPlatform.Linux },
        [ScriptRuntime.Sh] = new HashSet<ServerPlatform> { ServerPlatform.Linux },
        [ScriptRuntime.PowerShellCore] = new HashSet<ServerPlatform> { ServerPlatform.Linux, ServerPlatform.Windows },
        [ScriptRuntime.WindowsPowerShell] = new HashSet<ServerPlatform> { ServerPlatform.Windows },
        [ScriptRuntime.PythonCross] = new HashSet<ServerPlatform> { ServerPlatform.Linux, ServerPlatform.Windows }
    };
}
