using System;
using System.Globalization;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Utilities;

public static class NumberFormat
{
    // Los textos del panel están en español, así que los números también: "1.204" y "97,2".
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-ES");

    public static string Count(int valor)
    {
        return valor.ToString("N0", Cultura);
    }

    public static string Percent(double valor)
    {
        return valor.ToString("0.#", Cultura);
    }

    // Un total de cero no es un error: todavía no hay nada que medir y se lee como cero.
    public static string Share(int parte, int total)
    {
        return total <= 0 ? "0" : Percent(parte * 100d / total);
    }

    public static double Fraction(int parte, int total)
    {
        return total <= 0 ? 0 : parte / (double)total;
    }

    public static double Ratio(double parte, double total)
    {
        return total <= 0 ? 0 : Math.Clamp(parte / total, 0, 1);
    }

    public static string Fixed(double valor)
    {
        return valor.ToString("0.00", Cultura);
    }

    public static string Bytes(long valor)
    {
        if (valor <= 0)
            return "0 B";

        string[] unidades = ["B", "KB", "MB", "GB", "TB", "PB"];
        var escala = 0;
        double magnitud = valor;

        while (magnitud >= 1024 && escala < unidades.Length - 1)
        {
            magnitud /= 1024;
            escala++;
        }

        var formato = escala == 0 || magnitud >= 100 ? "0" : "0.#";
        return string.Create(Cultura, $"{magnitud.ToString(formato, Cultura)} {unidades[escala]}");
    }
}
