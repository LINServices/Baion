using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Contracts.Services;

namespace LIN.Cloud.Enterprise.Baion.Agent.Services.Tests.Fakes;

internal sealed class FakeInspector : IServiceInspector
{
    public Func<string?, ServiceQueryOutcome<IReadOnlyList<ServiceSummary>>>? OnList { get; init; }

    public Func<string, ServiceQueryOutcome<ServiceDetail>>? OnDescribe { get; init; }

    public Func<string, int, DateTimeOffset?, ServiceQueryOutcome<ServiceLogPage>>? OnLogs { get; init; }

    public Func<string, ServiceControlAction, ServiceQueryOutcome<ServiceDetail>>? OnControl { get; init; }

    public ServerPlatform Platform => ServerPlatform.Linux;

    public Task<ServiceQueryOutcome<IReadOnlyList<ServiceSummary>>> ListAsync(string? nameFilter, CancellationToken cancellationToken)
    {
        return Task.FromResult(OnList!(nameFilter));
    }

    public Task<ServiceQueryOutcome<ServiceDetail>> DescribeAsync(string serviceId, CancellationToken cancellationToken)
    {
        return Task.FromResult(OnDescribe!(serviceId));
    }

    public Task<ServiceQueryOutcome<ServiceLogPage>> GetLogsAsync(string serviceId, int maxLines, DateTimeOffset? since, CancellationToken cancellationToken)
    {
        return Task.FromResult(OnLogs!(serviceId, maxLines, since));
    }

    public Task<ServiceQueryOutcome<ServiceDetail>> ControlAsync(string serviceId, ServiceControlAction action, CancellationToken cancellationToken)
    {
        return Task.FromResult(OnControl!(serviceId, action));
    }
}
