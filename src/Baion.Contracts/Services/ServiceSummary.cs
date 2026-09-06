using Baion.Contracts.Enums;

namespace Baion.Contracts.Services;

/// <summary>Un servicio del sistema tal como aparece en el listado del servidor.</summary>
/// <param name="Id">Nombre de la unidad de systemd (<c>nginx.service</c>) o del servicio de Windows (<c>Spooler</c>).</param>
/// <param name="DisplayName">Nombre legible; en Linux suele coincidir con la descripción de la unidad.</param>
/// <param name="State">Estado normalizado entre plataformas.</param>
/// <param name="RawState">Estado sin traducir tal como lo reporta el sistema (<c>active (running)</c>, <c>Running</c>).</param>
/// <param name="StartupMode">Arranque configurado.</param>
public record ServiceSummary(string Id, string DisplayName, ServiceRuntimeState State, string RawState, ServiceStartupMode StartupMode);
