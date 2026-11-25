using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Fila del listado de grupos con el reparto de sus miembros por plataforma.
public record ServerGroupSummary(Guid Id, string Name, string? Description, int MemberCount, int LinuxCount, int WindowsCount, DateTimeOffset CreatedAt);
