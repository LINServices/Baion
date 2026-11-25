using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Aviso de que un agente cambió de sitio. Lo emite la instancia que gana o pierde el socket, y sirve
// para que las demás desalojen la entrada que tuvieran de ese servidor.
public record AgentPresenceChanged(Guid TenantId, Guid ServerId, string InstanceId, bool Connected, DateTimeOffset OccurredAt);
