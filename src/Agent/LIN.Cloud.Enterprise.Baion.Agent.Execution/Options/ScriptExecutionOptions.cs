namespace LIN.Cloud.Enterprise.Baion.Agent.Execution;

public class ScriptExecutionOptions
{
    public const string SectionName = "Execution";

    // Vacío significa la carpeta temporal del sistema.
    public string WorkingRoot { get; set; } = string.Empty;

    // Tope de concurrencia mientras todavía no hay sesión que lo fije.
    public int FallbackMaxConcurrentExecutions { get; set; } = 4;

    // Tamaño del bloque con el que se lee la salida del proceso.
    public int OutputChunkChars { get; set; } = 4096;
}
