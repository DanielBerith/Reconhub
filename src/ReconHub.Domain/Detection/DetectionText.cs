using System.Globalization;

namespace ReconHub.Domain.Detection;

internal static class DetectionText
{
    /// <summary>Formats a duration compactly, e.g. "15m" or "1h 2m 5s".</summary>
    public static string Duration(TimeSpan duration)
    {
        duration = duration.Duration();
        var parts = new List<string>(4);
        if (duration.Days > 0)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture, $"{duration.Days}d"));
        }

        if (duration.Hours > 0)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture, $"{duration.Hours}h"));
        }

        if (duration.Minutes > 0)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture, $"{duration.Minutes}m"));
        }

        if (duration.Seconds > 0 || parts.Count == 0)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture, $"{duration.Seconds}s"));
        }

        return string.Join(' ', parts);
    }

    /// <summary>Formats a timestamp as UTC, e.g. "2026-11-26 09:15:00Z".</summary>
    public static string Timestamp(DateTimeOffset value) => value.ToString("u", CultureInfo.InvariantCulture);
}
