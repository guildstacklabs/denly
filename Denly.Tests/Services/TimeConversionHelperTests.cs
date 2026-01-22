using Denly.Services;

namespace Denly.Tests.Services;

/// <summary>
/// Tests for TimeConversionHelper methods.
/// These tests verify the time conversion logic used in Calendar.razor.
/// </summary>
public class TimeConversionHelperTests
{
    #region ConvertTo12Hour Tests

    [Theory]
    [InlineData(0, 12, "AM")]   // Midnight
    [InlineData(1, 1, "AM")]
    [InlineData(11, 11, "AM")]
    [InlineData(12, 12, "PM")]  // Noon
    [InlineData(13, 1, "PM")]
    [InlineData(23, 11, "PM")]
    public void ConvertTo12Hour_StandardHours_ReturnsCorrectValues(int hour24, int expectedHour12, string expectedAmPm)
    {
        // Act
        var (hour12, amPm) = TimeConversionHelper.ConvertTo12Hour(hour24);

        // Assert
        Assert.Equal(expectedHour12, hour12);
        Assert.Equal(expectedAmPm, amPm);
    }

    [Fact]
    public void ConvertTo12Hour_Midnight_Returns12AM()
    {
        // Act
        var (hour12, amPm) = TimeConversionHelper.ConvertTo12Hour(0);

        // Assert
        Assert.Equal(12, hour12);
        Assert.Equal("AM", amPm);
    }

    [Fact]
    public void ConvertTo12Hour_Noon_Returns12PM()
    {
        // Act
        var (hour12, amPm) = TimeConversionHelper.ConvertTo12Hour(12);

        // Assert
        Assert.Equal(12, hour12);
        Assert.Equal("PM", amPm);
    }

    [Fact]
    public void ConvertTo12Hour_NegativeHour_HandlesGracefully()
    {
        // The modulo operation should normalize negative hours
        // -1 hour = 23:00 = 11 PM
        var (hour12, amPm) = TimeConversionHelper.ConvertTo12Hour(-1);

        Assert.Equal(11, hour12);
        Assert.Equal("PM", amPm);
    }

    [Fact]
    public void ConvertTo12Hour_HourOver23_HandlesGracefully()
    {
        // 25 hours = 1:00 AM (wraps around)
        var (hour12, amPm) = TimeConversionHelper.ConvertTo12Hour(25);

        Assert.Equal(1, hour12);
        Assert.Equal("AM", amPm);
    }

    #endregion

    #region ConvertTo24Hour Tests

    [Theory]
    [InlineData(12, "AM", 0)]   // Midnight
    [InlineData(1, "AM", 1)]
    [InlineData(11, "AM", 11)]
    [InlineData(12, "PM", 12)]  // Noon
    [InlineData(1, "PM", 13)]
    [InlineData(11, "PM", 23)]
    public void ConvertTo24Hour_StandardHours_ReturnsCorrectValues(int hour12, string amPm, int expectedHour24)
    {
        // Act
        var hour24 = TimeConversionHelper.ConvertTo24Hour(hour12, amPm);

        // Assert
        Assert.Equal(expectedHour24, hour24);
    }

    [Fact]
    public void ConvertTo24Hour_12AM_ReturnsMidnight()
    {
        // Act
        var hour24 = TimeConversionHelper.ConvertTo24Hour(12, "AM");

        // Assert
        Assert.Equal(0, hour24);
    }

    [Fact]
    public void ConvertTo24Hour_12PM_ReturnsNoon()
    {
        // Act
        var hour24 = TimeConversionHelper.ConvertTo24Hour(12, "PM");

        // Assert
        Assert.Equal(12, hour24);
    }

    [Fact]
    public void ConvertTo24Hour_CaseInsensitive_AM()
    {
        // Act
        var result1 = TimeConversionHelper.ConvertTo24Hour(9, "AM");
        var result2 = TimeConversionHelper.ConvertTo24Hour(9, "am");
        var result3 = TimeConversionHelper.ConvertTo24Hour(9, "Am");

        // Assert
        Assert.Equal(9, result1);
        Assert.Equal(9, result2);
        Assert.Equal(9, result3);
    }

    [Fact]
    public void ConvertTo24Hour_CaseInsensitive_PM()
    {
        // Act
        var result1 = TimeConversionHelper.ConvertTo24Hour(3, "PM");
        var result2 = TimeConversionHelper.ConvertTo24Hour(3, "pm");
        var result3 = TimeConversionHelper.ConvertTo24Hour(3, "Pm");

        // Assert
        Assert.Equal(15, result1);
        Assert.Equal(15, result2);
        Assert.Equal(15, result3);
    }

    [Fact]
    public void ConvertTo24Hour_NullAmPm_TreatsAsAM()
    {
        // Act
        var hour24 = TimeConversionHelper.ConvertTo24Hour(9, null);

        // Assert - null should be treated as AM (isPm = false)
        Assert.Equal(9, hour24);
    }

    #endregion

    #region Round-Trip Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(18)]
    [InlineData(23)]
    public void RoundTrip_24To12To24_PreservesOriginalHour(int originalHour24)
    {
        // Act
        var (hour12, amPm) = TimeConversionHelper.ConvertTo12Hour(originalHour24);
        var resultHour24 = TimeConversionHelper.ConvertTo24Hour(hour12, amPm);

        // Assert
        Assert.Equal(originalHour24, resultHour24);
    }

    #endregion

    #region GetLocalStartTime Tests

    [Fact]
    public void GetLocalStartTime_UtcKind_ConvertsToLocal()
    {
        // Arrange
        var utcTime = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = TimeConversionHelper.GetLocalStartTime(utcTime);

        // Assert
        Assert.Equal(DateTimeKind.Local, result.Kind);
        // The actual hour depends on timezone, but it should be different if not in UTC
    }

    [Fact]
    public void GetLocalStartTime_LocalKind_ReturnsUnchanged()
    {
        // Arrange
        var localTime = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Local);

        // Act
        var result = TimeConversionHelper.GetLocalStartTime(localTime);

        // Assert
        Assert.Equal(DateTimeKind.Local, result.Kind);
        Assert.Equal(localTime, result);
    }

    [Fact]
    public void GetLocalStartTime_UnspecifiedKind_TreatsAsLocal()
    {
        // Arrange
        var unspecifiedTime = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Unspecified);

        // Act
        var result = TimeConversionHelper.GetLocalStartTime(unspecifiedTime);

        // Assert
        Assert.Equal(DateTimeKind.Local, result.Kind);
        // Should preserve the same hour/minute since Unspecified is treated as Local
        Assert.Equal(10, result.Hour);
        Assert.Equal(0, result.Minute);
    }

    #endregion
}
