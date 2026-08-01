namespace MediaHub.Models;

/// <summary>
/// Lightweight metadata about what a download would produce, shown in the
/// preview card before the download actually starts.
/// </summary>
public sealed class MediaPreview
{
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public TimeSpan? Duration { get; init; }
    public string? QualityText { get; init; }
    public string Platform { get; init; } = string.Empty;

    /// <summary>
    /// "12:34" or "1:02:03"; empty when the duration is unknown.
    /// </summary>
    public string DurationText
    {
        get
        {
            if (Duration is not { } d)
                return string.Empty;

            var total = (int)d.TotalSeconds;
            return total >= 3600
                ? $"{total / 3600}:{total % 3600 / 60:00}:{total % 60:00}"
                : $"{total / 60}:{total % 60:00}";
        }
    }
}
