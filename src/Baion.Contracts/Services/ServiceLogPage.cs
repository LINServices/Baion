using System.Collections.Generic;

namespace Baion.Contracts.Services;

/// <summary>Instantánea de las últimas líneas de log de un servicio.</summary>
/// <param name="Lines">Líneas de la más antigua a la más reciente.</param>
/// <param name="Truncated">El agente recortó el resultado para no pasarse del tamaño máximo de trama.</param>
public record ServiceLogPage(IReadOnlyList<ServiceLogLine> Lines, bool Truncated);
