using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

// Reparto de los miembros de un grupo según puedan o no recibir un script.
public record GroupTargetPartition(IReadOnlyList<GroupMemberTarget> Dispatchable, IReadOnlyList<GroupMemberTarget> SkippedIncompatible, IReadOnlyList<GroupMemberTarget> SkippedDisabled);
