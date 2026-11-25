using System;
using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Services;

public record ServiceDetail(string Id, string DisplayName, string? Description, ServiceRuntimeState State, string RawState, string? SubState, ServiceStartupMode StartupMode, int? MainProcessId, DateTimeOffset? ActiveSince, long? MemoryBytes, string? ExecPath, int? ExitCode, IReadOnlyList<string> Dependencies);
