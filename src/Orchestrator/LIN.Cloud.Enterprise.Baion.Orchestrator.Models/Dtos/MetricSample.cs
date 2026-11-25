using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Messages;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

/// <summary>
/// Muestra recibida de un agente, ya asociada a su tenant y servidor, en espera de persistirse.
/// </summary>
public record MetricSample(Guid TenantId, Guid ServerId, MetricsReportMessage Report);
