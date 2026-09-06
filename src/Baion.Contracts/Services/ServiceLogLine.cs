using System;

namespace Baion.Contracts.Services;

/// <summary>Una línea de log de un servicio. <paramref name="Timestamp"/> y <paramref name="Level"/> son null si el origen no los aporta.</summary>
public record ServiceLogLine(DateTimeOffset? Timestamp, string? Level, string Message);
