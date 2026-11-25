namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

public class SchedulerOptions
{
    public const string SectionName = "Orchestrator:Scheduler";

    public bool Enabled { get; set; } = true;

    // Marca la precisión con la que se respeta el cron.
    public int TickSeconds { get; set; } = 15;

    // Tope de tareas que una instancia reserva por vuelta.
    public int MaxTasksPerTick { get; set; } = 50;

    // Tope de entregas pendientes que se reintentan por vuelta.
    public int MaxPendingDispatchesPerTick { get; set; } = 200;
}
