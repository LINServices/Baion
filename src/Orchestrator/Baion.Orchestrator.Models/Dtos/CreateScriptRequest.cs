using Baion.Contracts.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Datos para dar de alta un script. El checksum lo calcula el servidor sobre el contenido.</summary>
public record CreateScriptRequest(string Name, string? Description, string Content, ScriptRuntime Runtime, int DefaultTimeoutSeconds);
