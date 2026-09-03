using System;
using Baion.Contracts.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Agent.Execution.Implementations;

internal class WindowsScriptExecutor(IOptions<ScriptExecutionOptions> options, ILogger<WindowsScriptExecutor> logger) : ProcessScriptExecutor(options, logger)
{
    public override ServerPlatform Platform => ServerPlatform.Windows;

    // -ExecutionPolicy Bypass porque el script lo materializa el propio agente en una carpeta suya:
    // la política de firmas no aporta nada aquí y sí impediría ejecutarlo en la mayoría de servidores.
    protected override (string FileName, string Arguments) BuildCommand(ScriptRuntime runtime, string scriptPath) => runtime switch
    {
        ScriptRuntime.PowerShellCore => ("pwsh.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File {Quote(scriptPath)}"),
        ScriptRuntime.WindowsPowerShell => ("powershell.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File {Quote(scriptPath)}"),
        ScriptRuntime.PythonCross => ("python.exe", Quote(scriptPath)),
        _ => throw new NotSupportedException($"El intérprete {runtime} no está soportado en Windows.")
    };

    protected override string GetScriptExtension(ScriptRuntime runtime) => runtime switch
    {
        ScriptRuntime.PythonCross => ".py",
        _ => ".ps1"
    };

    private static string Quote(string path) => $"\"{path}\"";
}
