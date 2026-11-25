using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.SystemServices;

public record ServiceLogPage(IReadOnlyList<ServiceLogLine> Lines, bool Truncated);
