using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Services;

public record ServiceSummary(string Id, string DisplayName, ServiceRuntimeState State, string RawState, ServiceStartupMode StartupMode);
