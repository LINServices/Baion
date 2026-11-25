using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record DispatchGroupResult(Guid BatchId, int Total, int Dispatched, int SkippedIncompatible, int SkippedDisabled, int Failed, IReadOnlyList<BatchTargetResult> Targets);
