using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;

/// <summary>
/// Consultas de solo lectura de grupos de servidores para el panel.
/// </summary>
public interface IServerGroupQueries
{
    /// <summary>
    /// Grupos del tenant actual con el recuento de miembros por plataforma.
    /// </summary>
    Task<IReadOnlyList<ServerGroupSummary>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ficha de un grupo con sus miembros, o null si no existe en el tenant.
    /// </summary>
    Task<ServerGroupDetail?> GetAsync(Guid groupId, CancellationToken cancellationToken = default);
}
