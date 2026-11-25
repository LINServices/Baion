using System;
using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.SystemServices;

public record ServiceDetail(string Id, string DisplayName, string? Description, string State, string RawState, string? SubState, string StartupMode, int? MainProcessId, DateTimeOffset? ActiveSince, long? MemoryBytes, string? ExecPath, int? ExitCode, IReadOnlyList<string> Dependencies);
