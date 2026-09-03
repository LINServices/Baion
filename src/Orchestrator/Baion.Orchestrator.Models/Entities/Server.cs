using System;
using System.Collections.Generic;
using Baion.Contracts.Enums;
using Baion.Orchestrator.Models.Enums;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Servidor gestionado sobre el que corre un agente de Baion.</summary>
public class Server : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string Hostname { get; set; } = string.Empty;

    public ServerKind Kind { get; set; }

    public ServerPlatform Platform { get; set; }

    public ServerStatus Status { get; set; } = ServerStatus.Provisioning;

    public string? AgentVersion { get; set; }

    /// <summary>Identificador estable de la máquina. Hace idempotente el reenrolamiento tras una reinstalación.</summary>
    public string MachineId { get; set; } = string.Empty;

    /// <summary>SHA-256 de la credencial permanente del agente; null mientras no se haya enrolado.</summary>
    public string? AgentTokenHash { get; set; }

    /// <summary>RID del agente instalado (ej. linux-x64, win-x64); determina el binario de auto-actualización.</summary>
    public string? RuntimeIdentifier { get; set; }

    /// <summary>Instancia del orquestador que mantiene el socket abierto en este momento.</summary>
    public string? OrchestratorInstanceId { get; set; }

    public DateTimeOffset? ConnectedAt { get; set; }

    public DateTimeOffset? LastSeenAt { get; set; }

    /// <summary>Tope de ejecuciones simultáneas que el agente acepta.</summary>
    public int MaxConcurrentExecutions { get; set; } = 4;

    public ICollection<ServerGroupMember> GroupMemberships { get; set; } = [];

    public ICollection<ScriptExecution> Executions { get; set; } = [];
}
