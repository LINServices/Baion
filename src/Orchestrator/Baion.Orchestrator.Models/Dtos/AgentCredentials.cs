namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Credenciales que el agente presenta en las cabeceras al abrir el socket.</summary>
public record AgentCredentials(string? EnrollmentToken, string? AgentToken);
