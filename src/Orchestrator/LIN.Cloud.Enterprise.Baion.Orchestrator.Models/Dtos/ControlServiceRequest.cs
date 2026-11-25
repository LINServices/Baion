using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

/// <summary>
/// Cuerpo de la petición para aplicar una acción de control sobre un servicio de un servidor.
/// </summary>
public record ControlServiceRequest(ServiceControlAction Action);
