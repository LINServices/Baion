using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

public class Server : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string Hostname { get; set; } = string.Empty;

    public ServerKind Kind { get; set; }

    public ServerPlatform Platform { get; set; }

    public ServerStatus Status { get; set; } = ServerStatus.Provisioning;

    public string? AgentVersion { get; set; }

    // Identificador estable de la máquina. Hace idempotente el reenrolamiento tras una reinstalación.
    public string MachineId { get; set; } = string.Empty;

    // SHA-256 de la credencial permanente del agente; null mientras no se haya enrolado.
    public string? AgentTokenHash { get; set; }

    // RID del agente instalado (ej. linux-x64, win-x64); determina el binario de auto-actualización.
    public string? RuntimeIdentifier { get; set; }

    // Instancia del orquestador que mantiene el socket abierto en este momento.
    public string? OrchestratorInstanceId { get; set; }

    public DateTimeOffset? ConnectedAt { get; set; }

    public DateTimeOffset? LastSeenAt { get; set; }

    public int MaxConcurrentExecutions { get; set; } = 4;

    public ICollection<ServerGroupMember> GroupMemberships { get; set; } = [];

    public ICollection<ScriptExecution> Executions { get; set; } = [];
}
