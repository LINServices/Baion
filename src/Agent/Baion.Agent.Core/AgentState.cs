using System;
using System.Text.Json.Serialization;

namespace Baion.Agent.Core;

/// <summary>Estado que el agente conserva entre arranques.</summary>
public record AgentState
{
    /// <summary>Identificador estable de la máquina. Permite reenrolar sin duplicar el servidor.</summary>
    public string MachineId { get; init; } = string.Empty;

    /// <summary>Servidor asignado en el enrolamiento.</summary>
    public Guid? ServerId { get; init; }

    /// <summary>Credencial permanente con la que el agente reconecta.</summary>
    public string? AgentToken { get; init; }

    [JsonIgnore]
    public bool IsEnrolled => ServerId is not null && !string.IsNullOrWhiteSpace(AgentToken);
}
