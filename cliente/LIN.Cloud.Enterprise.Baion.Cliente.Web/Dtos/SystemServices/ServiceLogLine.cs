using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.SystemServices;

public record ServiceLogLine(DateTimeOffset? Timestamp, string? Level, string Message);
