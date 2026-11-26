using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Auth;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Common;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Dashboard;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Enrollment;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Executions;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scheduling;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scripts;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.ServerGroups;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Servers;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.SystemServices;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Results;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Services;

/// <summary>
/// Acceso del panel a la API del orquestador.
/// </summary>
public interface IBaionApiClient
{
    /// <summary>
    /// Autentica al usuario y devuelve su token de acceso.
    /// </summary>
    Task<ApiResult<AuthenticationResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Cifras de cabecera del panel.
    /// </summary>
    Task<ApiResult<DashboardSummary>> GetDashboardSummaryAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Servidores gestionados del tenant.
    /// </summary>
    Task<ApiResult<IReadOnlyList<ServerSummary>>> GetServersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Despacha un script a un servidor y devuelve el identificador de la ejecución.
    /// </summary>
    Task<ApiResult<ScriptExecutionDispatched>> DispatchAsync(DispatchScriptRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Emite un token de instalación. Su valor en claro solo se devuelve en esta respuesta.
    /// </summary>
    Task<ApiResult<EnrollmentTokenIssued>> CreateEnrollmentTokenAsync(CreateEnrollmentTokenRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Tokens de instalación del tenant, sin su valor.
    /// </summary>
    Task<ApiResult<IReadOnlyList<EnrollmentTokenSummary>>> GetEnrollmentTokensAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Revoca un token de instalación.
    /// </summary>
    Task<ApiResult<bool>> RevokeEnrollmentTokenAsync(Guid tokenId, CancellationToken cancellationToken);

    /// <summary>
    /// Un servidor con su última lectura de métricas.
    /// </summary>
    Task<ApiResult<ServerDetail>> GetServerAsync(Guid serverId, CancellationToken cancellationToken);

    /// <summary>
    /// Página del histórico de métricas de un servidor, de la muestra más reciente a la más antigua.
    /// </summary>
    Task<ApiResult<PagedResult<MetricReading>>> GetServerMetricsAsync(Guid serverId, DateTimeOffset? since, DateTimeOffset? until, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Desactiva un servidor: el orquestador corta la conexión de su agente y no lo readmite.
    /// </summary>
    Task<ApiResult<ServerSummary>> DisableServerAsync(Guid serverId, CancellationToken cancellationToken);

    /// <summary>
    /// Reactiva un servidor desactivado para que su agente pueda volver.
    /// </summary>
    Task<ApiResult<ServerSummary>> EnableServerAsync(Guid serverId, CancellationToken cancellationToken);

    /// <summary>
    /// Servicios del sistema de un servidor; <paramref name="filter"/> acota por subcadena del nombre.
    /// </summary>
    Task<ApiResult<IReadOnlyList<ServiceSummary>>> GetServerServicesAsync(Guid serverId, string? filter, CancellationToken cancellationToken);

    /// <summary>
    /// Detalle de un servicio concreto de un servidor.
    /// </summary>
    Task<ApiResult<ServiceDetail>> GetServerServiceAsync(Guid serverId, string serviceId, CancellationToken cancellationToken);

    /// <summary>
    /// Instantánea de los últimos logs de un servicio, opcionalmente acotada a partir de <paramref name="since"/>.
    /// </summary>
    Task<ApiResult<ServiceLogPage>> GetServerServiceLogsAsync(Guid serverId, string serviceId, int maxLines, DateTimeOffset? since, CancellationToken cancellationToken);

    /// <summary>
    /// Aplica una acción de control sobre un servicio y devuelve su detalle resultante. Requiere rol Admin.
    /// </summary>
    Task<ApiResult<ServiceDetail>> ControlServerServiceAsync(Guid serverId, string serviceId, string action, CancellationToken cancellationToken);

    /// <summary>
    /// Página del catálogo de scripts, filtrada opcionalmente por nombre.
    /// </summary>
    Task<ApiResult<PagedResult<ScriptListItem>>> GetScriptsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Un script con su contenido.
    /// </summary>
    Task<ApiResult<ScriptDetail>> GetScriptAsync(Guid scriptId, CancellationToken cancellationToken);

    /// <summary>
    /// Da de alta un script en el catálogo.
    /// </summary>
    Task<ApiResult<ScriptDetail>> CreateScriptAsync(CreateScriptRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Edita un script del catálogo. La versión y el checksum solo suben si cambia el contenido.
    /// </summary>
    Task<ApiResult<ScriptSummary>> UpdateScriptAsync(Guid scriptId, UpdateScriptRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Página del historial de ejecuciones, de la más reciente a la más antigua.
    /// </summary>
    Task<ApiResult<PagedResult<ScriptExecutionListItem>>> GetExecutionsAsync(Guid? serverId, Guid? scriptId, string? status, DateTimeOffset? since, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Una ejecución con su salida completa.
    /// </summary>
    Task<ApiResult<ScriptExecutionDetail>> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken);

    /// <summary>
    /// Grupos de servidores del tenant, con el recuento de miembros por plataforma.
    /// </summary>
    Task<ApiResult<IReadOnlyList<ServerGroupSummary>>> GetServerGroupsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Un grupo con sus miembros.
    /// </summary>
    Task<ApiResult<ServerGroupDetail>> GetServerGroupAsync(Guid groupId, CancellationToken cancellationToken);

    /// <summary>
    /// Crea un grupo de servidores. Requiere rol Admin.
    /// </summary>
    Task<ApiResult<ServerGroupDetail>> CreateServerGroupAsync(CreateServerGroupRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Edita un grupo; la lista de miembros reemplaza a la anterior por completo. Requiere rol Admin.
    /// </summary>
    Task<ApiResult<ServerGroupDetail>> UpdateServerGroupAsync(Guid groupId, UpdateServerGroupRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Elimina un grupo; falla si alguna tarea programada lo usa. Requiere rol Admin.
    /// </summary>
    Task<ApiResult<bool>> DeleteServerGroupAsync(Guid groupId, CancellationToken cancellationToken);

    /// <summary>
    /// Despacha un script a todos los miembros de un grupo y devuelve el resultado por servidor.
    /// </summary>
    Task<ApiResult<DispatchGroupResult>> DispatchGroupAsync(DispatchGroupRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Un lote de ejecuciones con sus contadores y las ejecuciones que lo componen.
    /// </summary>
    Task<ApiResult<ExecutionBatchDetail>> GetExecutionBatchAsync(Guid batchId, CancellationToken cancellationToken);

    /// <summary>
    /// Tareas programadas del tenant.
    /// </summary>
    Task<ApiResult<IReadOnlyList<ScheduledTaskListItem>>> GetScheduledTasksAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Da de alta una tarea programada sobre un script y un servidor o un grupo.
    /// </summary>
    Task<ApiResult<ScheduledTaskSummary>> CreateScheduledTaskAsync(CreateScheduledTaskRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Reactiva una tarea programada y recalcula su próximo disparo.
    /// </summary>
    Task<ApiResult<ScheduledTaskSummary>> EnableScheduledTaskAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>
    /// Pausa una tarea programada.
    /// </summary>
    Task<ApiResult<ScheduledTaskSummary>> DisableScheduledTaskAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>
    /// Dispara una tarea programada al instante y devuelve cuántos destinos se despacharon, fallaron u omitieron.
    /// </summary>
    Task<ApiResult<ScheduledTaskTriggered>> TriggerScheduledTaskAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>
    /// Ficha de una tarea programada.
    /// </summary>
    Task<ApiResult<ScheduledTaskSummary>> GetScheduledTaskAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>
    /// Reemplaza la configuración de una tarea programada.
    /// </summary>
    Task<ApiResult<ScheduledTaskSummary>> UpdateScheduledTaskAsync(Guid taskId, UpdateScheduledTaskRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Elimina una tarea programada. Su historial de ejecuciones se conserva sin tarea asociada.
    /// </summary>
    Task<ApiResult<bool>> DeleteScheduledTaskAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>
    /// Últimas ejecuciones lanzadas por una tarea programada.
    /// </summary>
    Task<ApiResult<IReadOnlyList<ScheduledTaskExecutionItem>>> GetScheduledTaskExecutionsAsync(Guid taskId, int take, CancellationToken cancellationToken);
}
