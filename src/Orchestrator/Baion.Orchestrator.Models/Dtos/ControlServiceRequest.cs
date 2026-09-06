using Baion.Contracts.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Cuerpo de la petición para aplicar una acción de control sobre un servicio de un servidor.</summary>
public record ControlServiceRequest(ServiceControlAction Action);
