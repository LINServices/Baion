using System;
using Baion.Contracts.Enums;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Ficha de un script sin su contenido.</summary>
public record ScriptSummary(Guid Id, string Name, ScriptRuntime Runtime, int Version, string Checksum, int DefaultTimeoutSeconds);
