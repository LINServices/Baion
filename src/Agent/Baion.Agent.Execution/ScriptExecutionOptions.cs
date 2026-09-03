namespace Baion.Agent.Execution;

/// <summary>Configuración de la ejecución de scripts en el agente.</summary>
public class ScriptExecutionOptions
{
    /// <summary>Carpeta donde se materializan los scripts antes de ejecutarlos. Vacío significa la temporal del sistema.</summary>
    public string WorkingRoot { get; set; } = string.Empty;

    /// <summary>Tope de concurrencia si todavía no hay sesión que lo fije.</summary>
    public int FallbackMaxConcurrentExecutions { get; set; } = 4;

    /// <summary>Tamaño del bloque con el que se lee la salida del proceso.</summary>
    public int OutputChunkChars { get; set; } = 4096;

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "Execution";
}
