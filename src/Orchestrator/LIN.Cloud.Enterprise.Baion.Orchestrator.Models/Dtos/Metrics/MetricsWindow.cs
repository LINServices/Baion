using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Ventana temporal de un histórico de métricas; cada extremo nulo no acota ese lado.
public record MetricsWindow(DateTimeOffset? Since, DateTimeOffset? Until);
