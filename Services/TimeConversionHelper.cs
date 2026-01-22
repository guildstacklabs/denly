namespace Denly.Services;

/// <summary>
/// Helper class for time conversion operations.
/// Extracted for testability from Calendar.razor.
/// </summary>
public static class TimeConversionHelper
{
    /// <summary>
    /// Converts a 24-hour format hour to 12-hour format with AM/PM.
    /// </summary>
    /// <param name="hour24">Hour in 24-hour format (0-23)</param>
    /// <returns>Tuple of (hour in 12-hour format, "AM" or "PM")</returns>
    public static (int hour12, string amPm) ConvertTo12Hour(int hour24)
    {
        var normalized = ((hour24 % 24) + 24) % 24;
        var amPm = normalized >= 12 ? "PM" : "AM";
        var hour12 = normalized % 12;
        if (hour12 == 0) hour12 = 12;
        return (hour12, amPm);
    }

    /// <summary>
    /// Converts a 12-hour format hour with AM/PM to 24-hour format.
    /// </summary>
    /// <param name="hour12">Hour in 12-hour format (1-12)</param>
    /// <param name="amPm">"AM" or "PM"</param>
    /// <returns>Hour in 24-hour format (0-23)</returns>
    public static int ConvertTo24Hour(int hour12, string? amPm)
    {
        var normalizedHour = hour12 % 12;
        var isPm = string.Equals(amPm, "PM", StringComparison.OrdinalIgnoreCase);
        return isPm ? normalizedHour + 12 : normalizedHour;
    }

    /// <summary>
    /// Gets the local start time from an event, handling different DateTimeKind values.
    /// </summary>
    /// <param name="startsAt">The event start time</param>
    /// <returns>DateTime normalized to local time</returns>
    public static DateTime GetLocalStartTime(DateTime startsAt)
    {
        return startsAt.Kind switch
        {
            DateTimeKind.Utc => startsAt.ToLocalTime(),
            DateTimeKind.Local => startsAt,
            _ => DateTime.SpecifyKind(startsAt, DateTimeKind.Local)
        };
    }
}
