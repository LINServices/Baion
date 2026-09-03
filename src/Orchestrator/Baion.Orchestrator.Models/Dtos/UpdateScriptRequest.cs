using Baion.Contracts.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>
/// Datos para editar un script existente. El checksum se recalcula y la versión sube solo si cambia
/// el contenido.
/// </summary>
public record UpdateScriptRequest(string Name, string? Description, string Content, ScriptRuntime Runtime, int DefaultTimeoutSeconds);
