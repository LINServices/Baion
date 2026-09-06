using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Enums;
using Baion.Contracts.Services;

namespace Baion.Agent.Services;

/// <summary>
/// Inspecciona y controla los servicios del sistema operativo del agente. Hay una implementación por
/// plataforma: systemd en Linux y el gestor de servicios (SCM) en Windows.
/// </summary>
public interface IServiceInspector
{
    /// <summary>Plataforma que atiende esta implementación.</summary>
    ServerPlatform Platform { get; }

    /// <summary>Lista los servicios del sistema, opcionalmente acotados por una subcadena del nombre.</summary>
    Task<ServiceQueryOutcome<IReadOnlyList<ServiceSummary>>> ListAsync(string? nameFilter, CancellationToken cancellationToken);

    /// <summary>Devuelve el detalle de un servicio concreto.</summary>
    Task<ServiceQueryOutcome<ServiceDetail>> DescribeAsync(string serviceId, CancellationToken cancellationToken);

    /// <summary>Devuelve una instantánea de las últimas líneas de log de un servicio.</summary>
    Task<ServiceQueryOutcome<ServiceLogPage>> GetLogsAsync(string serviceId, int maxLines, DateTimeOffset? since, CancellationToken cancellationToken);

    /// <summary>Aplica una acción sobre un servicio y devuelve su detalle una vez aplicada.</summary>
    Task<ServiceQueryOutcome<ServiceDetail>> ControlAsync(string serviceId, ServiceControlAction action, CancellationToken cancellationToken);
}
