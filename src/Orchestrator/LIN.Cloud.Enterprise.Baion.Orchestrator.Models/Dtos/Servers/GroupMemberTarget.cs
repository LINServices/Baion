using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Miembro de un grupo tal como lo necesita el despacho: sin más datos que los de decisión.
public record GroupMemberTarget(Guid ServerId, string ServerName, ServerPlatform Platform, ServerStatus Status);
