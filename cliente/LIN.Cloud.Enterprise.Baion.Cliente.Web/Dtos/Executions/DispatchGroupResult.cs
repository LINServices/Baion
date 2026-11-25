using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Executions;

public record DispatchGroupResult(Guid BatchId, int Total, int Dispatched, int SkippedIncompatible, int SkippedDisabled, int Failed, IReadOnlyList<BatchTargetResult> Targets);
