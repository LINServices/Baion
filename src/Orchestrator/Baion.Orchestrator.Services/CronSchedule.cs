using System;
using Cronos;

namespace Baion.Orchestrator.Services;

/// <summary>
/// Interpreta las expresiones cron de las tareas. Se admite el formato de cinco campos y el de seis con
/// segundos; se distinguen por el número de campos.
/// </summary>
public static class CronSchedule
{
    /// <summary>Calcula el siguiente disparo posterior a <paramref name="from"/>, o null si la expresión no vuelve a cumplirse.</summary>
    public static DateTimeOffset? GetNextOccurrence(string expression, string timeZoneId, DateTimeOffset from)
    {
        if (!TryParse(expression, out var cron) || !TryResolveTimeZone(timeZoneId, out var zone))
        {
            return null;
        }

        return cron.GetNextOccurrence(from, zone);
    }

    /// <summary>Indica si la expresión y la zona horaria son utilizables.</summary>
    public static bool IsValid(string expression, string timeZoneId) => TryParse(expression, out _) && TryResolveTimeZone(timeZoneId, out _);

    private static bool TryParse(string expression, out CronExpression cron)
    {
        cron = null!;

        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        var format = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 6 ? CronFormat.IncludeSeconds : CronFormat.Standard;

        try
        {
            cron = CronExpression.Parse(expression.Trim(), format);
            return true;
        }
        catch (CronFormatException)
        {
            return false;
        }
    }

    private static bool TryResolveTimeZone(string timeZoneId, out TimeZoneInfo zone)
    {
        zone = TimeZoneInfo.Utc;

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }
}
