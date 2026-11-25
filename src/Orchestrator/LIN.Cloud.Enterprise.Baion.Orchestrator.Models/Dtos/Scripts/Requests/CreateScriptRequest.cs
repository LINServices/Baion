using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Datos para dar de alta un script. El checksum lo calcula el servidor sobre el contenido.
public record CreateScriptRequest(string Name, string? Description, string Content, ScriptRuntime Runtime, int DefaultTimeoutSeconds);
