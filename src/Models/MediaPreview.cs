namespace MediaHub.Models;

/// <summary>
/// Lightweight metadata about what a download would produce, shown in the
/// preview card before the download actually starts.
/// </summary>
public sealed class MediaPreview
{
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;

    /// <summary>
    /// Long-form description when the platform exposes one separately from
    /// the title (YouTube and SoundCloud do; TikTok's caption is the title).
    /// </summary>
    public string Description { get; init; } = string.Empty;

    public TimeSpan? Duration { get; init; }
    public string? QualityText { get; init; }

    /// <summary>
    /// "12:34" or "1:02:03"; empty when the duration is unknown.
    /// </summary>
    public string DurationText => Duration is { } d ? FormatDuration(d) : string.Empty;

    /// <summary>
    /// Shared "m:ss" / "h:mm:ss" formatter used by the card and the
    /// resource-info rows.
    /// </summary>
    public static string FormatDuration(TimeSpan duration)
    {
        int total = (int)duration.TotalSeconds;
        return total >= 3600
            ? $"{total / 3600}:{total % 3600 / 60:00}:{total % 60:00}"
            : $"{total / 60}:{total % 60:00}";
    }
}
