using System;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Token de acceso emitido por Baion y su vencimiento.</summary>
public record AccessToken(string Value, DateTimeOffset ExpiresAt);
