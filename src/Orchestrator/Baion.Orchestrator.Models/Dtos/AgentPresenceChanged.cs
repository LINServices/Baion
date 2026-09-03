using System;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>
/// Aviso de que un agente cambió de sitio. Lo emite la instancia que gana o pierde el socket, y sirve
/// para que las demás desalojen la entrada que tuvieran de ese servidor.
/// </summary>
public record AgentPresenceChanged(Guid TenantId, Guid ServerId, string InstanceId, bool Connected, DateTimeOffset OccurredAt);
