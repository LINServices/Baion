using System.Collections.Generic;

namespace Baion.Cliente.Web.Components.Shared;

/// <summary>
/// Colección de <see cref="EstadoRaton"/> por clave, para botones que se repiten en un
/// bucle (una fila de tabla, una lista). El indexador crea el estado la primera vez que
/// se pide una clave, así que el marcado puede usarlo sin inicializar nada.
/// </summary>
public class EstadoRatonMapa
{
    private readonly Dictionary<object, EstadoRaton> _estados = [];

    public EstadoRaton this[object clave] =>
        _estados.TryGetValue(clave, out var estado) ? estado : _estados[clave] = new EstadoRaton();
}
