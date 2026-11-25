using System.Collections.Generic;
using System.Linq;
using LIN.Cloud.Enterprise.Baion.Contracts;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services.Implementations;

internal class GroupTargetResolver : IGroupTargetResolver
{
    public GroupTargetPartition Partition(ScriptRuntime runtime, IReadOnlyList<GroupMemberTarget> members)
    {
        var incompatible = members.Where(m => !ScriptRuntimeCompatibility.IsSupported(runtime, m.Platform)).ToList();
        var compatible = members.Except(incompatible).ToList();
        var disabled = compatible.Where(m => m.Status is ServerStatus.Disabled).ToList();
        var dispatchable = compatible.Except(disabled).ToList();

        return new GroupTargetPartition(dispatchable, incompatible, disabled);
    }
}
