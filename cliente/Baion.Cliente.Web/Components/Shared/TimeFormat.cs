using System;
using System.Globalization;

namespace Baion.Cliente.Web.Components.Shared;

/// <summary>
/// Fechas y duraciones escritas para una persona que está mirando el panel. Quien necesite la marca
/// exacta la tiene en la API; aquí interesa el orden de magnitud.
/// </summary>
public static class TimeFormat
{
    /// <summary>Cuánto hace que ocurrió algo.</summary>
    public static string Relative(DateTimeOffset? instante)
    {
        if (instante is not DateTimeOffset valor)
        {
            return "nunca";
        }

        var transcurrido = DateTimeOffset.UtcNow - valor;

        return transcurrido switch
        {
            { TotalSeconds: < 60 } => "hace segundos",
            { TotalMinutes: < 60 } => string.Format(Cultura, "hace {0} min", (int)transcurrido.TotalMinutes),
            { TotalHours: < 24 } => string.Format(Cultura, "hace {0} h", (int)transcurrido.TotalHours),
            _ => string.Format(Cultura, "hace {0} d", (int)transcurrido.TotalDays)
        };
    }

    /// <summary>Marca de tiempo completa, para cuando hay que contrastarla con un log.</summary>
    public static string Absolute(DateTimeOffset? instante) => instante is DateTimeOffset valor ? valor.ToString("dd/MM/yyyy HH:mm:ss", Cultura) : "—";

    /// <summary>Duración con la precisión que se puede leer de un vistazo.</summary>
    public static string Duration(TimeSpan? intervalo)
    {
        if (intervalo is not TimeSpan valor || valor < TimeSpan.Zero)
        {
            return "—";
        }

        return valor switch
        {
            { TotalSeconds: < 1 } => string.Format(Cultura, "{0:0} ms", valor.TotalMilliseconds),
            { TotalSeconds: < 60 } => string.Format(Cultura, "{0:0.0} s", valor.TotalSeconds),
            { TotalMinutes: < 60 } => string.Format(Cultura, "{0} min {1} s", (int)valor.TotalMinutes, valor.Seconds),
            _ => string.Format(Cultura, "{0} h {1} min", (int)valor.TotalHours, valor.Minutes)
        };
    }

    /// <summary>Lo que duró un tramo; si todavía no ha terminado, lo que lleva durando.</summary>
    public static string Elapsed(DateTimeOffset? inicio, DateTimeOffset? fin)
    {
        if (inicio is not DateTimeOffset arranque)
        {
            return "—";
        }

        return Duration((fin ?? DateTimeOffset.UtcNow) - arranque);
    }

    // Los textos del panel están en español, así que los números también: "1,4 s" y no "1.4 s".
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-ES");
}
