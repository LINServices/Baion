using System;
using System.Collections.Generic;

namespace Baion.Cliente.Web.Models;

/// <summary>
/// Reflejo de los contratos de la API del orquestador. Se declaran aquí a propósito en lugar de referenciar
/// sus proyectos: el panel es una aplicación aparte que habla HTTP, y compartir ensamblados la ataría a la
/// versión exacta del servidor.
/// </summary>
public record LoginRequest(string TenantSlug, string Email, string Password);

/// <summary>Respuesta de un login correcto.</summary>
public record AuthenticationResult(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, Guid TenantId, Guid UserId, string Email, IReadOnlyList<string> Roles);

/// <summary>Servidor gestionado tal y como lo lista la API.</summary>
public record ServerSummary(Guid Id, string Name, string Hostname, string Kind, string Platform, string Status, string? AgentVersion, string? RuntimeIdentifier, string? OrchestratorInstanceId, DateTimeOffset? ConnectedAt, DateTimeOffset? LastSeenAt, int MaxConcurrentExecutions);

/// <summary>Última muestra de métricas conocida de un servidor.</summary>
public record ServerMetricsSnapshot(DateTimeOffset CapturedAt, double CpuUsagePercent, int CpuCoreCount, long MemoryTotalBytes, long MemoryAvailableBytes)
{
    public long MemoryUsedBytes => MemoryTotalBytes - MemoryAvailableBytes;

    public double MemoryUsagePercent => MemoryTotalBytes <= 0 ? 0 : MemoryUsedBytes * 100d / MemoryTotalBytes;
}

/// <summary>Servidor con su última lectura de métricas.</summary>
public record ServerDetail(ServerSummary Server, ServerMetricsSnapshot? LastMetrics);

/// <summary>Una muestra del histórico de métricas de un servidor, con el detalle por volumen.</summary>
public record MetricReading(DateTimeOffset CapturedAt, double CpuUsagePercent, int CpuCoreCount, double? LoadAverage1m, long MemoryTotalBytes, long MemoryAvailableBytes, IReadOnlyList<MetricDiskReading> Disks)
{
    public long MemoryUsedBytes => MemoryTotalBytes - MemoryAvailableBytes;

    public double MemoryUsagePercent => MemoryTotalBytes <= 0 ? 0 : MemoryUsedBytes * 100d / MemoryTotalBytes;
}

/// <summary>Uso de un volumen concreto dentro de una muestra de métricas.</summary>
public record MetricDiskReading(string Name, string MountPoint, long TotalBytes, long AvailableBytes)
{
    public long UsedBytes => TotalBytes - AvailableBytes;

    public double UsagePercent => TotalBytes <= 0 ? 0 : UsedBytes * 100d / TotalBytes;
}

/// <summary>Cifras de cabecera del panel.</summary>
public record DashboardSummary(int TotalServers, int OnlineServers, int OfflineServers, int TotalScripts, int RunningExecutions, int ExecutionsLast24Hours, int FailedLast24Hours);

/// <summary>Datos para emitir un token de instalación.</summary>
public record CreateEnrollmentTokenRequest(string Name, string DefaultServerKind, DateTimeOffset? ExpiresAt, int? MaxUses);

/// <summary>Token recién emitido. El valor en claro solo llega aquí y no se puede volver a consultar.</summary>
public record EnrollmentTokenIssued(Guid Id, string Token, DateTimeOffset? ExpiresAt);

/// <summary>Ficha de un token de instalación, sin su valor.</summary>
public record EnrollmentTokenSummary(Guid Id, string Name, string DefaultServerKind, DateTimeOffset? ExpiresAt, int? MaxUses, int UseCount, DateTimeOffset? RevokedAt, DateTimeOffset CreatedAt, bool IsUsable);

/// <summary>Cuerpo de error que devuelve la API cuando algo va mal.</summary>
public record ApiProblem(string? Title, string? Detail, int? Status);

/// <summary>Página de resultados tal y como la devuelven los listados de la API.</summary>
public record PagedResult<TItem>(IReadOnlyList<TItem> Items, int Page, int PageSize, int TotalCount, int TotalPages, bool HasPrevious, bool HasNext);

/// <summary>Script del catálogo, en su versión de listado (sin el contenido).</summary>
public record ScriptListItem(Guid Id, string Name, string? Description, string Runtime, int Version, string Checksum, int DefaultTimeoutSeconds, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

/// <summary>Script del catálogo con su contenido.</summary>
public record ScriptDetail(Guid Id, string Name, string? Description, string Runtime, int Version, string Checksum, int DefaultTimeoutSeconds, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, string Content);

/// <summary>Alta de un script en el catálogo.</summary>
public record CreateScriptRequest(string Name, string? Description, string Content, string Runtime, int DefaultTimeoutSeconds);

/// <summary>
/// Edición de un script del catálogo. Idéntico al alta salvo el nombre. La versión y el checksum solo
/// cambian si cambia el contenido: tocar solo nombre, descripción, intérprete o timeout deja la versión igual.
/// </summary>
public record UpdateScriptRequest(string Name, string? Description, string Content, string Runtime, int DefaultTimeoutSeconds);

/// <summary>Script tal y como lo devuelve la API tras crearlo o editarlo: sin contenido, con la versión y el checksum ya resueltos.</summary>
public record ScriptSummary(Guid Id, string Name, string Runtime, int Version, string Checksum, int DefaultTimeoutSeconds);

/// <summary>Orden de ejecutar un script sobre un servidor concreto.</summary>
/// <param name="TimeoutSeconds">Sobrescribe el timeout por defecto del script cuando tiene valor.</param>
public record DispatchScriptRequest(Guid ScriptId, Guid ServerId, string Mode, int? TimeoutSeconds, string? WorkingDirectory, IReadOnlyDictionary<string, string>? EnvironmentVariables);

/// <summary>Confirmación de que la orden salió hacia el agente.</summary>
public record ScriptExecutionDispatched(Guid ExecutionId, string Status);

/// <summary>Ejecución de un script, en su versión de listado (sin la salida).</summary>
public record ScriptExecutionListItem(Guid Id, Guid ServerId, string ServerName, Guid ScriptId, string ScriptName, string Status, string Mode, int? ExitCode, DateTimeOffset QueuedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, Guid? ChainRunId, Guid? ScheduledTaskId);

/// <summary>Ejecución de un script con su salida completa.</summary>
public record ScriptExecutionDetail(Guid Id, Guid ServerId, string ServerName, Guid ScriptId, string ScriptName, string Status, string Mode, int? ExitCode, string? StdOut, string? StdErr, string? ErrorMessage, DateTimeOffset QueuedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt);
