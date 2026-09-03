using System;
using System.Globalization;

namespace Baion.Cliente.Web.Components.Shared;

/// <summary>
/// Cifras del panel escritas en la convención local: coma decimal y punto de millares. Se formatean
/// siempre aquí y nunca a mano, para que un mismo dato se lea igual en una tarjeta y en una tabla.
/// </summary>
public static class NumberFormat
{
    /// <summary>Cantidad entera con separador de millares.</summary>
    public static string Count(int valor) => valor.ToString("N0", Cultura);

    /// <summary>Porcentaje con un decimal, sin el símbolo: la unidad va aparte, en superíndice.</summary>
    public static string Percent(double valor) => valor.ToString("0.#", Cultura);

    /// <summary>
    /// Porcentaje que representa <paramref name="parte"/> sobre <paramref name="total"/>. Un total de
    /// cero no es un error: significa que todavía no hay nada que medir y se lee como cero.
    /// </summary>
    public static string Share(int parte, int total) => total <= 0 ? "0" : Percent(parte * 100d / total);

    /// <summary>Fracción de 0 a 1 de <paramref name="parte"/> sobre <paramref name="total"/>, para barras y arcos.</summary>
    public static double Fraction(int parte, int total) => total <= 0 ? 0 : parte / (double)total;

    /// <summary>
    /// Fracción de 0 a 1 de <paramref name="parte"/> sobre <paramref name="total"/> con magnitudes continuas
    /// (bytes, porcentajes), ya recortada al rango para alimentar barras y arcos.
    /// </summary>
    public static double Ratio(double parte, double total) => total <= 0 ? 0 : Math.Clamp(parte / total, 0, 1);

    /// <summary>Número con dos decimales, para valores que no son porcentaje: carga media, factores…</summary>
    public static string Fixed(double valor) => valor.ToString("0.00", Cultura);

    /// <summary>Tamaño en la unidad binaria que se lee de un vistazo: «5,4 GB», «928 MB», «0 B».</summary>
    public static string Bytes(long valor)
    {
        if (valor <= 0)
        {
            return "0 B";
        }

        string[] unidades = ["B", "KB", "MB", "GB", "TB", "PB"];
        var escala = 0;
        double magnitud = valor;

        while (magnitud >= 1024 && escala < unidades.Length - 1)
        {
            magnitud /= 1024;
            escala++;
        }

        var formato = escala == 0 || magnitud >= 100 ? "0" : "0.#";
        return string.Format(Cultura, "{0} {1}", magnitud.ToString(formato, Cultura), unidades[escala]);
    }

    // Los textos del panel están en español, así que los números también: "1.204" y "97,2".
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-ES");
}
