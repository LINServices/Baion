namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

public class ScriptEventOptions
{
    public const string SectionName = "Orchestrator:ScriptEvents";

    // A diferencia de las métricas, perder un fragmento deja la salida incompleta: el buzón es holgado y
    // su llenado se registra como error.
    public int QueueCapacity { get; set; } = 50_000;

    public int BatchSize { get; set; } = 200;

    public int BatchWindowMilliseconds { get; set; } = 250;
}
