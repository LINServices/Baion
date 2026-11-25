using System;
using Cronos;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

// Admite el formato de cinco campos y el de seis con segundos; se distinguen por el número de campos.
public static class CronSchedule
{
    // Devuelve null si la expresión o la zona no son válidas, o si no vuelve a cumplirse.
    public static DateTimeOffset? GetNextOccurrence(string expression, string timeZoneId, DateTimeOffset from)
    {
        if (!TryParse(expression, out var cron) || !TryResolveTimeZone(timeZoneId, out var zone))
            return null;

        return cron.GetNextOccurrence(from, zone);
    }

    public static bool IsValid(string expression, string timeZoneId)
    {
        return TryParse(expression, out _) && TryResolveTimeZone(timeZoneId, out _);
    }

    private static bool TryParse(string expression, out CronExpression cron)
    {
        cron = null!;

        if (string.IsNullOrWhiteSpace(expression))
            return false;

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
            return false;

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
