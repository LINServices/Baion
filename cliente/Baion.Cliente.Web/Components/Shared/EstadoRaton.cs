namespace Baion.Cliente.Web.Components.Shared;

/// <summary>
/// Estado de interacción del ratón sobre un control. El marcado engancha los cuatro
/// eventos del puntero a <see cref="Entrar"/>, <see cref="Salir"/>, <see cref="Pulsar"/>
/// y <see cref="Soltar"/>, y pinta <see cref="Clase"/> junto a la clase base del botón;
/// el aspecto de <c>.is-hover</c> / <c>.is-pressed</c> vive en <c>app.css</c>.
/// </summary>
public class EstadoRaton
{
    private bool _encima;
    private bool _pulsado;

    public void Entrar() => _encima = true;

    public void Salir()
    {
        _encima = false;
        _pulsado = false;
    }

    public void Pulsar() => _pulsado = true;

    public void Soltar() => _pulsado = false;

    /// <summary>Sufijo de clase para el estado actual: vacío, <c>is-hover</c> o <c>is-pressed</c>.</summary>
    public string Clase => _pulsado ? "is-pressed" : _encima ? "is-hover" : string.Empty;
}
