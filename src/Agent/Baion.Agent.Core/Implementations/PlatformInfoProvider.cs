using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Baion.Contracts.Enums;

namespace Baion.Agent.Core.Implementations;

internal class PlatformInfoProvider : IPlatformInfoProvider
{
    public ServerPlatform Platform { get; } = ResolvePlatform();

    public string RuntimeIdentifier { get; } = RuntimeInformation.RuntimeIdentifier;

    public string AgentVersion { get; } = ResolveAgentVersion();

    public string Hostname { get; } = Environment.MachineName;

    public int CoreCount { get; } = Environment.ProcessorCount;

    // Es la memoria que ve el runtime, que bajo contenedor coincide con el límite del cgroup.
    // La fase de métricas la mide por plataforma; aquí solo sirve para describir la máquina.
    public long TotalMemoryBytes { get; } = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;

    private static ServerPlatform ResolvePlatform()
    {
        if (OperatingSystem.IsLinux())
        {
            return ServerPlatform.Linux;
        }

        if (OperatingSystem.IsWindows())
        {
            return ServerPlatform.Windows;
        }

        throw new PlatformNotSupportedException($"El agente de Baion solo corre sobre Linux y Windows; se detectó {RuntimeInformation.OSDescription}.");
    }

    private static string ResolveAgentVersion() => typeof(PlatformInfoProvider).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0";
}
