using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Entities;

namespace Baion.Orchestrator.Persistence;

/// <summary>
/// Acceso a los datos del enrolamiento y la presencia de agentes. Las búsquedas por credencial ignoran
/// el filtro de tenant a propósito: son justamente las que resuelven a qué tenant pertenece quien se conecta.
/// </summary>
public interface IAgentRepository
{
    /// <summary>Busca un token de instalación por su hash, sin filtro de tenant.</summary>
    Task<EnrollmentToken?> FindEnrollmentTokenAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Obtiene un token de instalación por identificador, sin filtro de tenant.</summary>
    Task<EnrollmentToken?> FindEnrollmentTokenByIdAsync(Guid tokenId, CancellationToken cancellationToken);

    /// <summary>Busca el servidor dueño de una credencial de agente, sin filtro de tenant.</summary>
    Task<Server?> FindByAgentTokenAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Busca un servidor por su identificador de máquina dentro de un tenant, sin filtro de tenant.</summary>
    Task<Server?> FindByMachineIdAsync(Guid tenantId, string machineId, CancellationToken cancellationToken);

    /// <summary>Obtiene un servidor por identificador dentro de un tenant, sin filtro de tenant.</summary>
    Task<Server?> FindByIdAsync(Guid tenantId, Guid serverId, CancellationToken cancellationToken);

    /// <summary>Indica si el tenant ya tiene un servidor con ese nombre.</summary>
    Task<bool> NameExistsAsync(Guid tenantId, string name, CancellationToken cancellationToken);

    /// <summary>Añade un servidor recién enrolado.</summary>
    Task AddAsync(Server server);

    /// <summary>
    /// Suelta la presencia de un servidor cuyo socket acaba de morir. Es una escritura condicional a
    /// propósito: el cierre suele ser consecuencia de una desactivación, y leer-modificar-escribir aquí
    /// pisaría el estado que la desactivación acaba de dejar puesto.
    /// </summary>
    Task MarkDisconnectedAsync(Guid tenantId, Guid serverId, DateTimeOffset lastSeenAt, CancellationToken cancellationToken);

    /// <summary>
    /// Marca como desconectados los servidores que esta instancia tenía registrados. Se llama al arrancar,
    /// para limpiar la presencia que quedó colgada tras una caída, y al parar de forma ordenada.
    /// </summary>
    Task<int> ReleaseInstanceServersAsync(string orchestratorInstanceId, CancellationToken cancellationToken);
}
