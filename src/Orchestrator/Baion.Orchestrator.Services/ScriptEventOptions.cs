namespace Baion.Orchestrator.Services;

/// <summary>Parámetros de la ingesta de novedades de ejecución.</summary>
public class ScriptEventOptions
{
    /// <summary>
    /// Tope de novedades en espera. A diferencia de las métricas, aquí perder un fragmento deja la salida
    /// incompleta, así que el buzón es holgado y su llenado se registra como error.
    /// </summary>
    public int QueueCapacity { get; set; } = 50_000;

    public int BatchSize { get; set; } = 200;

    public int BatchWindowMilliseconds { get; set; } = 250;

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "Orchestrator:ScriptEvents";
}
