using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Services;

public record ServiceLogPage(IReadOnlyList<ServiceLogLine> Lines, bool Truncated);
