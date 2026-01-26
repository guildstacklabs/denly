using System.Globalization;

namespace Denly.Services;

public class DenTimeService : IDenTimeService
{
    private readonly IDenService _denService;

    public DenTimeService(IDenService denService)
    {
        _denService = denService;
    }

    public async Task<TimeZoneInfo> GetDenTimeZoneAsync(CancellationToken cancellationToken = default)
    {
        var den = await _denService.GetCurrentDenAsync();
        if (den == null || string.IsNullOrWhiteSpace(den.TimeZone))
        {
            return TimeZoneInfo.Local;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(den.TimeZone);
        }
        catch
        {
            return TimeZoneInfo.Local;
        }
    }

    public DateTime ConvertToDenTime(DateTime value, TimeZoneInfo? timeZone = null)
    {
        var tz = timeZone ?? TimeZoneInfo.Local;
        var input = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Local)
            : value;
        return TimeZoneInfo.ConvertTime(input, tz);
    }

    public string FormatTime(DateTime value, string format, TimeZoneInfo? timeZone = null)
    {
        var denTime = ConvertToDenTime(value, timeZone);
        return denTime.ToString(format, CultureInfo.CurrentCulture);
    }

    public string FormatDayHeader(DateTime value, TimeZoneInfo? timeZone = null)
    {
        var denTime = ConvertToDenTime(value, timeZone);
        return denTime.ToString("dddd, MMM d", CultureInfo.CurrentCulture);
    }
}
