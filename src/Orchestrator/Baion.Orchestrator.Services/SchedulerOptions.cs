namespace Baion.Orchestrator.Services;

/// <summary>Parámetros del planificador de tareas.</summary>
public class SchedulerOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Cada cuánto se busca trabajo vencido. Marca la precisión con la que se respeta el cron.</summary>
    public int TickSeconds { get; set; } = 15;

    /// <summary>Tope de tareas que una instancia reserva por vuelta.</summary>
    public int MaxTasksPerTick { get; set; } = 50;

    /// <summary>Tope de entregas pendientes que se reintentan por vuelta.</summary>
    public int MaxPendingDispatchesPerTick { get; set; } = 200;

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "Orchestrator:Scheduler";
}
