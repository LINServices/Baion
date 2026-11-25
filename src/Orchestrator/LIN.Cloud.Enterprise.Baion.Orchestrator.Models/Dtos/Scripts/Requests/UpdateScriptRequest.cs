using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Datos para editar un script existente. El checksum se recalcula y la versión sube solo si cambia
// el contenido.
public record UpdateScriptRequest(string Name, string? Description, string Content, ScriptRuntime Runtime, int DefaultTimeoutSeconds);
