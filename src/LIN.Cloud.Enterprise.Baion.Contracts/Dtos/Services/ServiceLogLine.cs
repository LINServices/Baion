using System;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Services;

public record ServiceLogLine(DateTimeOffset? Timestamp, string? Level, string Message);
