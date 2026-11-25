using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Fila de un listado de scripts; deliberadamente sin el contenido.
public record ScriptListItem(Guid Id, string Name, string? Description, ScriptRuntime Runtime, int Version, string Checksum, int DefaultTimeoutSeconds, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
