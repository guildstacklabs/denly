namespace Denly.Services;

public interface IDenTimeService
{
    Task<TimeZoneInfo> GetDenTimeZoneAsync(CancellationToken cancellationToken = default);
    DateTime ConvertToDenTime(DateTime value, TimeZoneInfo? timeZone = null);
    string FormatTime(DateTime value, string format, TimeZoneInfo? timeZone = null);
    string FormatDayHeader(DateTime value, TimeZoneInfo? timeZone = null);
}
