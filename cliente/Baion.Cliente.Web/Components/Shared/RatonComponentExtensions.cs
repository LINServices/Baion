using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Baion.Cliente.Web.Components.Shared;

/// <summary>
/// Adjunta los cuatro eventos del puntero de un botón a un <see cref="EstadoRaton"/> con
/// una sola marca: <c>@attributes="this.EventosRaton(_raton)"</c>. El estado resultante
/// se pinta con <c>@_raton.Clase</c> junto a la clase base del botón.
/// </summary>
public static class RatonComponentExtensions
{
    public static Dictionary<string, object> EventosRaton(this ComponentBase receptor, EstadoRaton estado) => new()
    {
        ["onmouseenter"] = EventCallback.Factory.Create(receptor, estado.Entrar),
        ["onmouseleave"] = EventCallback.Factory.Create(receptor, estado.Salir),
        ["onmousedown"] = EventCallback.Factory.Create(receptor, estado.Pulsar),
        ["onmouseup"] = EventCallback.Factory.Create(receptor, estado.Soltar),
    };
}
