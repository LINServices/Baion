namespace LIN.Cloud.Enterprise.Baion.Agent.Services.Implementations;

// Una unidad tal como la lista systemctl list-units.
internal record SystemdUnit(string Unit, string Active, string Sub, string Description);
