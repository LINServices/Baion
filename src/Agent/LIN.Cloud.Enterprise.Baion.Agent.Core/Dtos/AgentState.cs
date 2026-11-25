using System;
using System.Text.Json.Serialization;

namespace LIN.Cloud.Enterprise.Baion.Agent.Core;

public record AgentState
{
    // Identificador estable de la máquina: permite reenrolar sin duplicar el servidor.
    public string MachineId { get; init; } = string.Empty;

    public Guid? ServerId { get; init; }

    // Credencial permanente con la que el agente reconecta.
    public string? AgentToken { get; init; }

    [JsonIgnore]
    public bool IsEnrolled => ServerId is not null && !string.IsNullOrWhiteSpace(AgentToken);
}
