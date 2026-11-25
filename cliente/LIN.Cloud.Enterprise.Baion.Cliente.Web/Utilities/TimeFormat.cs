using System;
using System.Globalization;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Utilities;

public static class TimeFormat
{
    // Los textos del panel están en español, así que los números también: "1,4 s" y no "1.4 s".
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-ES");

    public static string Relative(DateTimeOffset? instante)
    {
        if (instante is not DateTimeOffset valor)
            return "nunca";

        var transcurrido = DateTimeOffset.UtcNow - valor;

        return transcurrido switch
        {
            { TotalSeconds: < 60 } => "hace segundos",
            { TotalMinutes: < 60 } => string.Create(Cultura, $"hace {(int)transcurrido.TotalMinutes} min"),
            { TotalHours: < 24 } => string.Create(Cultura, $"hace {(int)transcurrido.TotalHours} h"),
            _ => string.Create(Cultura, $"hace {(int)transcurrido.TotalDays} d")
        };
    }

    public static string Absolute(DateTimeOffset? instante)
    {
        return instante is DateTimeOffset valor ? valor.ToString("dd/MM/yyyy HH:mm:ss", Cultura) : "—";
    }

    public static string Duration(TimeSpan? intervalo)
    {
        if (intervalo is not TimeSpan valor || valor < TimeSpan.Zero)
            return "—";

        return valor switch
        {
            { TotalSeconds: < 1 } => string.Create(Cultura, $"{valor.TotalMilliseconds:0} ms"),
            { TotalSeconds: < 60 } => string.Create(Cultura, $"{valor.TotalSeconds:0.0} s"),
            { TotalMinutes: < 60 } => string.Create(Cultura, $"{(int)valor.TotalMinutes} min {valor.Seconds} s"),
            _ => string.Create(Cultura, $"{(int)valor.TotalHours} h {valor.Minutes} min")
        };
    }

    public static string Elapsed(DateTimeOffset? inicio, DateTimeOffset? fin)
    {
        if (inicio is not DateTimeOffset arranque)
            return "—";

        return Duration((fin ?? DateTimeOffset.UtcNow) - arranque);
    }
}
