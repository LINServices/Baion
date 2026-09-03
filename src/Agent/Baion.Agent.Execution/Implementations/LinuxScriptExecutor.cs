using System;
using System.IO;
using System.Runtime.Versioning;
using Baion.Contracts.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Agent.Execution.Implementations;

internal class LinuxScriptExecutor(IOptions<ScriptExecutionOptions> options, ILogger<LinuxScriptExecutor> logger) : ProcessScriptExecutor(options, logger)
{
    public override ServerPlatform Platform => ServerPlatform.Linux;

    protected override (string FileName, string Arguments) BuildCommand(ScriptRuntime runtime, string scriptPath) => runtime switch
    {
        ScriptRuntime.Bash => ("/bin/bash", Quote(scriptPath)),
        ScriptRuntime.Sh => ("/bin/sh", Quote(scriptPath)),
        ScriptRuntime.PowerShellCore => ("pwsh", $"-NoProfile -NonInteractive -File {Quote(scriptPath)}"),
        ScriptRuntime.PythonCross => ("python3", Quote(scriptPath)),
        _ => throw new NotSupportedException($"El intérprete {runtime} no está soportado en Linux.")
    };

    protected override string GetScriptExtension(ScriptRuntime runtime) => runtime switch
    {
        ScriptRuntime.PowerShellCore => ".ps1",
        ScriptRuntime.PythonCross => ".py",
        _ => ".sh"
    };

    /// <summary>Solo el dueño puede leer y ejecutar: el script puede traer credenciales en su contenido.</summary>
    [UnsupportedOSPlatform("windows")]
    protected override void PrepareScriptFile(string scriptPath) => File.SetUnixFileMode(scriptPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

    private static string Quote(string path) => $"\"{path}\"";
}
