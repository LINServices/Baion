using System;
using Baion.Contracts.Messages;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Muestra recibida de un agente, ya asociada a su tenant y servidor, en espera de persistirse.</summary>
public record MetricSample(Guid TenantId, Guid ServerId, MetricsReportMessage Report);
