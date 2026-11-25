using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

/// <summary>
/// Decide qué miembros de un grupo pueden recibir un script.
/// </summary>
public interface IGroupTargetResolver
{
    /// <summary>
    /// Reparte los miembros: el incompatible con el intérprete va a omitidos por incompatibilidad aunque
    /// esté deshabilitado; el deshabilitado compatible, a omitidos por deshabilitado; el resto es despachable.
    /// </summary>
    GroupTargetPartition Partition(ScriptRuntime runtime, IReadOnlyList<GroupMemberTarget> members);
}
