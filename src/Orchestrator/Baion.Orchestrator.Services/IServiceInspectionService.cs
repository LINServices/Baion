using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Contracts.Services;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Services;

/// <summary>
/// Consulta y controla los servicios del sistema (systemd en Linux, SCM en Windows) de un servidor del
/// tenant actual. Los datos se piden al agente en el momento por el socket; nada se persiste.
/// </summary>
public interface IServiceInspectionService
{
    /// <summary>Lista los servicios del servidor, opcionalmente acotando por una subcadena del nombre.</summary>
    Task<Result<IReadOnlyList<ServiceSummary>>> ListAsync(Guid serverId, string? nameFilter, CancellationToken cancellationToken);

    /// <summary>Obtiene el detalle de un servicio concreto del servidor.</summary>
    Task<Result<ServiceDetail>> DescribeAsync(Guid serverId, string serviceId, CancellationToken cancellationToken);

    /// <summary>
    /// Instantánea de los logs de un servicio: las últimas <paramref name="maxLines"/> líneas (o el valor
    /// por defecto), acotadas a partir de <paramref name="since"/> si se indica.
    /// </summary>
    Task<Result<ServiceLogPage>> GetLogsAsync(Guid serverId, string serviceId, int? maxLines, DateTimeOffset? since, CancellationToken cancellationToken);

    /// <summary>
    /// Aplica una acción de control sobre un servicio (arrancar, parar, reiniciar, habilitar, deshabilitar)
    /// y devuelve su detalle resultante. Requiere que el agente tenga privilegios en la máquina.
    /// </summary>
    Task<Result<ServiceDetail>> ControlAsync(Guid serverId, string serviceId, ServiceControlAction action, CancellationToken cancellationToken);
}
