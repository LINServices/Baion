using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Entities;

namespace Baion.Orchestrator.Persistence;

/// <summary>
/// Acceso a las tareas programadas. El barrido del planificador es de instancia, no de tenant, así que
/// esas consultas ignoran el filtro global a propósito y devuelven el <c>TenantId</c> de cada fila.
/// </summary>
public interface IScheduledTaskRepository
{
    /// <summary>Obtiene una tarea del tenant actual con su script o cadena y su destino.</summary>
    Task<ScheduledTask?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>Marca una tarea para inserción.</summary>
    Task AddAsync(ScheduledTask task);

    /// <summary>Tareas activas de todos los tenants cuyo próximo disparo ya venció.</summary>
    Task<IReadOnlyList<ScheduledTask>> GetDueAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Reserva el disparo avanzando <c>next_run_at</c> solo si sigue teniendo el valor esperado. Devuelve
    /// false cuando otra instancia se adelantó: es lo que impide que la tarea se dispare por duplicado.
    /// </summary>
    Task<bool> TryClaimAsync(Guid taskId, DateTimeOffset expectedNextRunAt, DateTimeOffset nextOccurrence, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Servidores de un grupo, con el grupo acotado a su tenant.</summary>
    Task<IReadOnlyList<Guid>> GetGroupServerIdsAsync(Guid tenantId, Guid serverGroupId, CancellationToken cancellationToken);
}
