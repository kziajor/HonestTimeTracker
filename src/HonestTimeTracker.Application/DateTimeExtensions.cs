namespace HonestTimeTracker.Application;

internal static class DateTimeExtensions
{
    public static DateTime TruncateToMinute(this DateTime dt) =>
        new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind);
}
