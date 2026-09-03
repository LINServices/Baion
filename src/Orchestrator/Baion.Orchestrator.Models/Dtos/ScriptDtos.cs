using System;
using Baion.Contracts.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Fila de un listado de scripts; deliberadamente sin el contenido.</summary>
public record ScriptListItem(Guid Id, string Name, string? Description, ScriptRuntime Runtime, int Version, string Checksum, int DefaultTimeoutSeconds, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

/// <summary>Ficha completa de un script, ya con su contenido.</summary>
public record ScriptDetail(Guid Id, string Name, string? Description, string Content, ScriptRuntime Runtime, int Version, string Checksum, int DefaultTimeoutSeconds, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
