namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.SystemServices;

// State y StartupMode llegan como texto camelCase:
// unknown | running | stopped | starting | stopping | failed y unknown | automatic | manual | disabled.
public record ServiceSummary(string Id, string DisplayName, string State, string RawState, string StartupMode);
