using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Cliente.Web.Models;

namespace Baion.Cliente.Web.Services;

/// <summary>Acceso del panel a la API del orquestador.</summary>
public interface IBaionApiClient
{
    /// <summary>Autentica al usuario y devuelve su token de acceso.</summary>
    Task<ApiResult<AuthenticationResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>Cifras de cabecera del panel.</summary>
    Task<ApiResult<DashboardSummary>> GetDashboardSummaryAsync(CancellationToken cancellationToken);

    /// <summary>Servidores gestionados del tenant.</summary>
    Task<ApiResult<IReadOnlyList<ServerSummary>>> GetServersAsync(CancellationToken cancellationToken);

    /// <summary>Despacha un script a un servidor y devuelve el identificador de la ejecución.</summary>
    Task<ApiResult<ScriptExecutionDispatched>> DispatchAsync(DispatchScriptRequest request, CancellationToken cancellationToken);

    /// <summary>Emite un token de instalación. Su valor en claro solo se devuelve en esta respuesta.</summary>
    Task<ApiResult<EnrollmentTokenIssued>> CreateEnrollmentTokenAsync(CreateEnrollmentTokenRequest request, CancellationToken cancellationToken);

    /// <summary>Tokens de instalación del tenant, sin su valor.</summary>
    Task<ApiResult<IReadOnlyList<EnrollmentTokenSummary>>> GetEnrollmentTokensAsync(CancellationToken cancellationToken);

    /// <summary>Revoca un token de instalación.</summary>
    Task<ApiResult<bool>> RevokeEnrollmentTokenAsync(Guid tokenId, CancellationToken cancellationToken);

    /// <summary>Un servidor con su última lectura de métricas.</summary>
    Task<ApiResult<ServerDetail>> GetServerAsync(Guid serverId, CancellationToken cancellationToken);

    /// <summary>Página del histórico de métricas de un servidor, de la muestra más reciente a la más antigua.</summary>
    Task<ApiResult<PagedResult<MetricReading>>> GetServerMetricsAsync(Guid serverId, DateTimeOffset? since, DateTimeOffset? until, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Desactiva un servidor: el orquestador corta la conexión de su agente y no lo readmite.</summary>
    Task<ApiResult<ServerSummary>> DisableServerAsync(Guid serverId, CancellationToken cancellationToken);

    /// <summary>Reactiva un servidor desactivado para que su agente pueda volver.</summary>
    Task<ApiResult<ServerSummary>> EnableServerAsync(Guid serverId, CancellationToken cancellationToken);

    /// <summary>Página del catálogo de scripts, filtrada opcionalmente por nombre.</summary>
    Task<ApiResult<PagedResult<ScriptListItem>>> GetScriptsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Un script con su contenido.</summary>
    Task<ApiResult<ScriptDetail>> GetScriptAsync(Guid scriptId, CancellationToken cancellationToken);

    /// <summary>Da de alta un script en el catálogo.</summary>
    Task<ApiResult<ScriptDetail>> CreateScriptAsync(CreateScriptRequest request, CancellationToken cancellationToken);

    /// <summary>Edita un script del catálogo. La versión y el checksum solo suben si cambia el contenido.</summary>
    Task<ApiResult<ScriptSummary>> UpdateScriptAsync(Guid scriptId, UpdateScriptRequest request, CancellationToken cancellationToken);

    /// <summary>Página del historial de ejecuciones, de la más reciente a la más antigua.</summary>
    Task<ApiResult<PagedResult<ScriptExecutionListItem>>> GetExecutionsAsync(Guid? serverId, Guid? scriptId, string? status, DateTimeOffset? since, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Una ejecución con su salida completa.</summary>
    Task<ApiResult<ScriptExecutionDetail>> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken);
}
