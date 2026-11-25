using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record ScriptChainSummary(Guid Id, string Name, bool IsActive, IReadOnlyList<ScriptChainStepSummary> Steps);
