using System;
using System.Collections.Generic;
using Baion.Contracts.Enums;
using Baion.Orchestrator.Models.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Ficha de un servidor gestionado, tal y como la muestra el panel.</summary>
public record ServerSummary(Guid Id, string Name, string Hostname, ServerKind Kind, ServerPlatform Platform, ServerStatus Status, string? AgentVersion, string? RuntimeIdentifier, string? OrchestratorInstanceId, DateTimeOffset? ConnectedAt, DateTimeOffset? LastSeenAt, int MaxConcurrentExecutions);

/// <summary>Última muestra de métricas conocida de un servidor.</summary>
public record ServerMetricsSnapshot(DateTimeOffset CapturedAt, double CpuUsagePercent, int CpuCoreCount, long MemoryTotalBytes, long MemoryAvailableBytes);

/// <summary>Servidor con su última lectura de métricas, si la tiene.</summary>
public record ServerDetail(ServerSummary Server, ServerMetricsSnapshot? LastMetrics);

/// <summary>Muestra de métricas de un servidor, con el detalle por volumen.</summary>
public record MetricReading(DateTimeOffset CapturedAt, double CpuUsagePercent, int CpuCoreCount, double? LoadAverage1m, long MemoryTotalBytes, long MemoryAvailableBytes, IReadOnlyList<MetricDiskReading> Disks);

/// <summary>Uso de un volumen dentro de una muestra de métricas.</summary>
public record MetricDiskReading(string Name, string MountPoint, long TotalBytes, long AvailableBytes);

/// <summary>Ventana temporal de un histórico de métricas; cada extremo nulo no acota ese lado.</summary>
public record MetricsWindow(DateTimeOffset? Since, DateTimeOffset? Until);

/// <summary>Cifras de cabecera del panel.</summary>
public record DashboardSummary(int TotalServers, int OnlineServers, int OfflineServers, int TotalScripts, int RunningExecutions, int ExecutionsLast24Hours, int FailedLast24Hours);
