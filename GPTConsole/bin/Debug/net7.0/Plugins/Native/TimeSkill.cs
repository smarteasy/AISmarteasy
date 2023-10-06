using System.ComponentModel;
using AISmarteasy.Core.Function;

namespace Plugins.Native.Skills;

public sealed class TimeSkill
{
    [SKFunction, Description("Get the current date")]
    public string Date(IFormatProvider? formatProvider = null) =>
        DateTimeOffset.Now.ToString("D", formatProvider);

    [SKFunction, Description("Get the current date")]
    public string Today(IFormatProvider? formatProvider = null) =>
        Date(formatProvider);

    [SKFunction, Description("Get the current date and time in the local time zone")]
    public string Now(IFormatProvider? formatProvider = null) =>
        DateTimeOffset.Now.ToString("f", formatProvider);

    [SKFunction, Description("Get the current UTC date and time")]
    public string UtcNow(IFormatProvider? formatProvider = null) =>
        DateTimeOffset.UtcNow.ToString("f", formatProvider);

    [SKFunction, Description("Get the current time")]
    public string Time(IFormatProvider? formatProvider = null) =>
        DateTimeOffset.Now.ToString("hh:mm:ss tt", formatProvider);

    [SKFunction, Description("Get the current year")]
    public string Year(IFormatProvider? formatProvider = null) =>
        DateTimeOffset.Now.ToString("yyyy", formatProvider);

    [SKFunction, Description("Get the current month name")]
    public string Month(IFormatProvider? formatProvider = null) =>
        DateTimeOffset.Now.ToString("MMMM", formatProvider);

    [SKFunction, Description("Get the current month number")]
    public string MonthNumber(IFormatProvider? formatProvider = null) =>
        DateTimeOffset.Now.ToString("MM", formatProvider);

    [SKFunction, Description("Get the current day of the month")]
    public string Day(IFormatProvider? formatProvider = null) =>
        DateTimeOffset.Now.ToString("dd", formatProvider);

    [SKFunction]
    [Description("Get the date offset by a provided number of days from today")]
    public string DaysAgo([Description("The number of days to offset from today"), SKName("input")] double daysOffset, IFormatProvider? formatProvider = null) =>
        DateTimeOffset.Now.AddDays(-daysOffset).ToString("D", formatProvider);

    [SKFunction, Description("Get the current day of the week")]
    public string DayOfWeek(IFormatProvider? formatProvider = null) =>
        DateTimeOffset.Now.ToString("dddd", formatProvider);

    [SKFunction, Description("Get the current clock hour")]
    public string Hour(IFormatProvider? formatProvider = null) =>
        DateTimeOffset.Now.ToString("h tt", formatProvider);

    [SKFunction, Description("Get the current clock 24-hour number")]
    public string HourNumber(IFormatProvider? formatProvider = null) =>
        DateTimeOffset.Now.ToString("HH", formatProvider);

    [SKFunction]
    [Description("Get the date of the last day matching the supplied week day name in English. Example: Che giorno era 'Martedi' scorso -> dateMatchingLastDayName 'Tuesday' => Tuesday, 16 May, 2023")]
    public string DateMatchingLastDayName(
        [Description("The day name to match"), SKName("input")] DayOfWeek dayName,
        IFormatProvider? formatProvider = null)
    {
        DateTimeOffset dateTime = DateTimeOffset.Now;

        for (int i = 1; i <= 7; ++i)
        {
            dateTime = dateTime.AddDays(-1);
            if (dateTime.DayOfWeek == dayName)
            {
                break;
            }
        }

        return dateTime.ToString("D", formatProvider);
    }

    [SKFunction, Description("Get the minutes on the current hour")]
    public string Minute(IFormatProvider? formatProvider = null) =>
        DateTimeOffset.Now.ToString("mm", formatProvider);

    [SKFunction, Description("Get the seconds on the current minute")]
    public string Second(IFormatProvider? formatProvider = null) =>
        DateTimeOffset.Now.ToString("ss", formatProvider);

  [SKFunction, Description("Get the local time zone offset from UTC")]
    public string TimeZoneOffset(IFormatProvider? formatProvider = null) =>
        DateTimeOffset.Now.ToString("%K", formatProvider);

    [SKFunction, Description("Get the local time zone name")]
    public string TimeZoneName() =>
        TimeZoneInfo.Local.DisplayName;
}
