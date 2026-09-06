namespace Baion.Agent.Services.Implementations;

/// <summary>Una unidad tal como la lista <c>systemctl list-units</c>.</summary>
internal record SystemdUnit(string Unit, string Active, string Sub, string Description);
