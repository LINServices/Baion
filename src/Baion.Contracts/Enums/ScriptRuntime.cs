namespace Baion.Contracts.Enums;

/// <summary>Intérprete con el que se ejecuta un script.</summary>
public enum ScriptRuntime
{
    Bash = 1,
    Sh = 2,
    PowerShellCore = 3,
    WindowsPowerShell = 4,
    PythonCross = 5
}
